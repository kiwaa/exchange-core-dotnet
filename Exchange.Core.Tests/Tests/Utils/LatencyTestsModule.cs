using Exchange.Core.Collections.Utils;
using Exchange.Core.Common;
using Exchange.Core.Common.Api;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Tests.Utils;
using HdrHistogram;
using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Tests.Utils
{
    public class LatencyTestsModule
    {
        private static ILog log = LogManager.GetLogger(typeof(LatencyTestsModule));

        private static readonly bool WRITE_HDR_HISTOGRAMS = false;

        public static void latencyTestImpl(PerformanceConfiguration performanceCfg,
                                           TestDataParameters testDataParameters,
                                           InitialStateConfiguration initialStateCfg,
                                           SerializationConfiguration serializationCfg,
                                           int warmupCycles)
        {

            int targetTps = 200_000; // transactions per second
            int targetTpsStep = 100_000;

            int warmupTps = 1_000_000;

            TestDataFutures testDataFutures = ExchangeTestContainer.prepareTestDataAsync(testDataParameters, 1);

            using (ExchangeTestContainer container = ExchangeTestContainer.create(performanceCfg, initialStateCfg, serializationCfg))
            {

                ExchangeApi api = container.api;
                IntHistogram hdrRecorder = new IntHistogram(int.MaxValue, 2);

                // TODO - first run should validate the output (orders are accepted and processed properly)

                Func<int, bool, bool> testIteration = (tps, warmup) =>
                {
                    try
                    {
                        container.loadSymbolsUsersAndPrefillOrdersNoLog(testDataFutures);

                        MultiSymbolGenResult genResult = testDataFutures.GenResult.Result;

                        CountdownEvent latchBenchmark = new CountdownEvent(genResult.BenchmarkCommandsSize);

                        container.consumer = (cmd, seq) =>
                        {
                            long latency = DateTime.UtcNow.Ticks - cmd.Timestamp;
                            hdrRecorder.RecordValue(Math.Min(latency, int.MaxValue));
                            latchBenchmark.Signal();
                        };

                        int nanosPerCmd = 1_000_000_000 / tps;
                        var sw = Stopwatch.StartNew();
                        //long startTimeMs = System.currentTimeMillis();

                        long plannedTimestamp = DateTime.UtcNow.Ticks;

                        foreach (ApiCommand cmd in genResult.ApiCommandsBenchmark.Result)
                        {
                            while (DateTime.UtcNow.Ticks < plannedTimestamp)
                            {
                                // spin until its time to send next command
                            }
                            cmd.Timestamp = plannedTimestamp;
                            api.submitCommand(cmd);
                            plannedTimestamp += nanosPerCmd;
                        }

                        latchBenchmark.Wait();
                        container.consumer = (cmd, seq) => { };

                        sw.Stop();
                        long processingTimeMs = sw.ElapsedMilliseconds; //System.currentTimeMillis() - startTimeMs;
                        float perfMt = (float)genResult.BenchmarkCommandsSize / (float)processingTimeMs / 1000.0f;
                        String tag = String.Format("%.3f MT/s", perfMt);
                        IntHistogram histogram = hdrRecorder;
                        log.Info($"{tag} {LatencyTools.createLatencyReportFast(histogram)}");

                        // compare orderBook state just to make sure all commands executed same way
                        foreach (var symbol in testDataFutures.CoreSymbolSpecifications.Result)
                        {
                            Assert.AreEqual(
                                testDataFutures.GenResult.Result.GenResults[symbol.SymbolId].FinalOrderBookSnapshot,
                                container.requestCurrentOrderBook(symbol.SymbolId));
                        }

                        // TODO compare events, balances, positions

                        if (WRITE_HDR_HISTOGRAMS)
                        {
                            TextWriter printStream = File.CreateText(DateTime.UtcNow.Ticks + "-" + perfMt + ".perc");
                            //log.info("HDR 50%:{}", hdr.getValueAtPercentile(50));
                            histogram.OutputPercentileDistribution(printStream, outputValueUnitScalingRatio: 1000.0);
                        }

                        container.resetExchangeCore();

                        GC.Collect();
                        Thread.Sleep(500);

                        // stop testing if median latency above 1 millisecond
                        return warmup || histogram.GetValueAtPercentile(50.0) < 10_000_000;

                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("", ex);
                    }
                };

                container.executeTestingThread(() =>
                {
                    log.Debug($"Warming up {warmupCycles} cycles...");
                    for (int i = 0; i < warmupCycles; i++)
                        testIteration(warmupTps, true);
                    log.Debug("Warmup done, starting tests");

                    return Enumerable.Range(0, 10000)
                            .Select(i => targetTps + targetTpsStep * i)
                            .Select(tps => testIteration(tps, false))
                            .All(x => x);
                });
            }
        }

        public static void individualLatencyTest(PerformanceConfiguration performanceConfiguration,
                                                 TestDataParameters testDataParameters,
                                                 InitialStateConfiguration initialStateConfiguration)
        {

            TestDataFutures testDataFutures = ExchangeTestContainer.prepareTestDataAsync(testDataParameters, 1);

            int[] minLatencies = new int[testDataParameters.TotalTransactionsNumber];
            Arrays.fill(minLatencies, int.MaxValue);

            using (ExchangeTestContainer container = ExchangeTestContainer.create(performanceConfiguration, initialStateConfiguration, SerializationConfiguration.DEFAULT))
            {

                // TODO - first run should validate the output (orders are accepted and processed properly)

                Func<int, bool> testIteration = (step) =>
                {

                    try
                    {
                        ExchangeApi api = container.api;

                        container.loadSymbolsUsersAndPrefillOrdersNoLog(testDataFutures);

                        MultiSymbolGenResult genResult = testDataFutures.GenResult.Result;

                        List<ApiCommand> apiCommandsBenchmark = genResult.ApiCommandsBenchmark.Result;
                        int[] latencies = new int[apiCommandsBenchmark.Count];
                        int[] matcherEvents = new int[apiCommandsBenchmark.Count];
                        int counter = 0;

                        long orderProgressCounter = 0;
                        container.consumer = (cmd, seq) =>
                        {
                            Interlocked.Exchange(ref orderProgressCounter, cmd.Timestamp);

                            long latency = DateTime.UtcNow.Ticks - cmd.Timestamp;
                            long lat = Math.Min(latency, int.MaxValue);
                            int i = counter++;

                            latencies[i] = (int)lat;


                            MatcherTradeEvent matcherEvent = cmd.MatcherEvent;
                            while (matcherEvent != null)
                            {
                                matcherEvent = matcherEvent.NextEvent;
                                matcherEvents[i]++;
                            }

                            if (cmd.ResultCode != CommandResultCode.SUCCESS)
                            {
                                throw new InvalidOperationException();
                            }
                        };

                        foreach (ApiCommand cmd in apiCommandsBenchmark)
                        {
                            long t = DateTime.UtcNow.Ticks;
                            cmd.Timestamp = t;
                            api.submitCommand(cmd);
                            while (orderProgressCounter != t)
                            {
                                // spin until command is processed
                            }
                        }

                        container.consumer = (cmd, seq) => { };

                        Dictionary<Type, IntHistogram> commandsClassLatencies = new Dictionary<Type, IntHistogram>();

                        IntHistogram placeIocLatencies = new IntHistogram(int.MaxValue, 2);
                        IntHistogram placeGtcLatencies = new IntHistogram(int.MaxValue, 2);
                        IntHistogram moveOrderEvts0 = new IntHistogram(int.MaxValue, 2);
                        IntHistogram moveOrderEvts1 = new IntHistogram(int.MaxValue, 2);
                        IntHistogram moveOrderEvts2 = new IntHistogram(int.MaxValue, 2);

                        IntHistogram minLatenciesHdr = new IntHistogram(int.MaxValue, 2);

                        // TODO change to case based
                        List<SlowCommandRecord> slowCommands = new List<SlowCommandRecord>(apiCommandsBenchmark.Count);

                        for (int i = 0; i < apiCommandsBenchmark.Count; i++)
                        {
                            int latency = latencies[i];
                            int minLatency = Math.Min(minLatencies[i], latency);
                            minLatencies[i] = minLatency;
                            minLatenciesHdr.RecordValue(minLatency);

                            int matcherEventsNum = matcherEvents[i];
                            ApiCommand apiCommand = apiCommandsBenchmark[i];
                            Type aClass = apiCommand.GetType();

                            IntHistogram hdrSvr;
                            if (commandsClassLatencies.TryGetValue(aClass, out IntHistogram value))
                            {
                                hdrSvr = value;
                            }
                            else
                            {
                                hdrSvr = commandsClassLatencies[aClass] = new IntHistogram(int.MaxValue, 2);
                            }
                            hdrSvr.RecordValue(minLatency);

                            slowCommands.Add(new SlowCommandRecord(minLatency, i, apiCommand, matcherEventsNum));

                            if (apiCommand is ApiPlaceOrder)
                            {
                                if (((ApiPlaceOrder)apiCommand).OrderType == OrderType.GTC)
                                {
                                    placeGtcLatencies.RecordValue(minLatency);
                                }
                                else
                                {
                                    placeIocLatencies.RecordValue(minLatency);
                                }
                            }
                            else if (apiCommand is ApiMoveOrder)
                            {
                                if (matcherEventsNum == 0)
                                {
                                    moveOrderEvts0.RecordValue(minLatency);
                                }
                                else if (matcherEventsNum == 1)
                                {
                                    moveOrderEvts1.RecordValue(minLatency);
                                }
                                else
                                {
                                    moveOrderEvts2.RecordValue(minLatency);
                                }
                            }

                        }
                        log.Info("command independent latencies:");
                        log.Info($"  Theoretical {LatencyTools.createLatencyReportFast(minLatenciesHdr)}");
                        foreach (var (cls, hdr) in commandsClassLatencies)
                        {
                            log.Info($"  {cls.Name} {LatencyTools.createLatencyReportFast(hdr)}");
                        }
                        log.Info($"  Place GTC   {LatencyTools.createLatencyReportFast(placeGtcLatencies)}");
                        log.Info($"  Place IOC   {LatencyTools.createLatencyReportFast(placeIocLatencies)}");
                        log.Info($"  Move 0  evt {LatencyTools.createLatencyReportFast(moveOrderEvts0)}");
                        log.Info($"  Move 1  evt {LatencyTools.createLatencyReportFast(moveOrderEvts1)}");
                        log.Info($"  Move 2+ evt {LatencyTools.createLatencyReportFast(moveOrderEvts2)}");

                        slowCommands.Sort(new ComparatorLatencyDesc());

                        log.Info("Slowest commands (theoretical):");
                        foreach (var p in slowCommands.Take(100))
                        {
                            var tmp = String.Format("%06X", p.SeqNumber);
                            var tmp2 = p.EventsNum > 1 ? String.Format("(%dns per matching)", p.MinLatency / p.EventsNum) : "";
                            log.Info($"{tmp}. {LatencyTools.formatNanos(p.MinLatency)} {p.ApiCommand} events:{p.EventsNum} {tmp2}");
                        }
                        container.resetExchangeCore();

                        GC.Collect();
                        Thread.Sleep(500);

                        // stop testing if median latency above 1 millisecond
                        return true;

                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("", ex);
                    }
                };

                // running tests
                container.executeTestingThread(() => Enumerable.Range(0, 32).Select(testIteration).All(x => x));
            }
        }

        private class ComparatorLatencyDesc : IComparer<SlowCommandRecord>
        {
            public int Compare([AllowNull] SlowCommandRecord x, [AllowNull] SlowCommandRecord y)
            {
                return -x.MinLatency.CompareTo(y.MinLatency);
            }
        }
        //private static Comparator<SlowCommandRecord> COMPARATOR_LATENCY_DESC = Comparator.< SlowCommandRecord > comparingInt(c->c.minLatency).reversed();


        public static void hiccupTestImpl(PerformanceConfiguration performanceConfiguration,
                                          TestDataParameters testDataParameters,
                                          InitialStateConfiguration initialStateConfiguration,
                                          int warmupCycles)
        {

            int targetTps = 500_000; // transactions per second

            // will print each occurrence if latency>0.2ms
            long hiccupThresholdNs = 200_000;

            TestDataFutures testDataFutures = ExchangeTestContainer.prepareTestDataAsync(testDataParameters, 1);

            using (ExchangeTestContainer container = ExchangeTestContainer.create(performanceConfiguration, initialStateConfiguration, SerializationConfiguration.DEFAULT))
            {

                ExchangeApi api = container.api;

                Func<int, SortedDictionary<DateTimeOffset, long>> testIteration = tps =>
                {
                    try
                    {
                        container.loadSymbolsUsersAndPrefillOrdersNoLog(testDataFutures);

                        MultiSymbolGenResult genResult = testDataFutures.GenResult.Result;

                        Dictionary<long, long> hiccupTimestampsNs = new Dictionary<long, long>(10000);
                        CountdownEvent latchBenchmark = new CountdownEvent(genResult.BenchmarkCommandsSize);

                        long nextHiccupAcceptTimestampNs = 0;

                        container.consumer = (cmd, seq) =>
                        {
                            long now = DateTime.UtcNow.Ticks;
                        // skip other messages in delayed group
                        if (now < nextHiccupAcceptTimestampNs)
                            {
                                return;
                            }
                            long diffNs = now - cmd.Timestamp;
                        // register hiccup timestamps
                        if (diffNs > hiccupThresholdNs)
                            {
                                hiccupTimestampsNs[cmd.Timestamp] = diffNs;
                                nextHiccupAcceptTimestampNs = cmd.Timestamp + diffNs;
                            }
                            latchBenchmark.Signal();
                        };

                        long startTimeNs = DateTime.UtcNow.Ticks;
                        long startTimeMs = DateTime.UtcNow.Ticks;
                        int nanosPerCmd = 1_000_000_000 / tps;

                        long plannedTimestamp = DateTime.UtcNow.Ticks;

                        foreach (ApiCommand cmd in genResult.ApiCommandsBenchmark.Result)
                        {
                            // spin until its time to send next command
                            while (DateTime.UtcNow.Ticks < plannedTimestamp) ;

                            cmd.Timestamp = plannedTimestamp;
                            api.submitCommand(cmd);
                            plannedTimestamp += nanosPerCmd;
                        }

                        latchBenchmark.Wait();

                        container.consumer = (cmd, seq) => { };

                        SortedDictionary<DateTimeOffset, long> sorted = new SortedDictionary<DateTimeOffset, long>();
                        // convert nanosecond timestamp into Instant
                        // not very precise, but for 1ms resolution is ok (0,05% accuracy is required)...
                        // delay (nanoseconds) merging as max value
                        foreach (var (eventTimestampNs, delay) in hiccupTimestampsNs)
                        {
                            var key = new DateTimeOffset(new DateTime(startTimeMs + (eventTimestampNs - startTimeNs)), TimeZoneInfo.Local.BaseUtcOffset);
                            if (sorted.TryGetValue(key, out long value))
                            {
                                sorted[key] = Math.Max(value, delay);
                            }
                            else
                            {
                                sorted[key] = delay;
                            }
                        }
                        container.resetExchangeCore();

                        GC.Collect();
                        Thread.Sleep(500);

                        return sorted;

                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("", ex);
                    }
                };

                container.executeTestingThread<object>(() =>
                {

                    log.Debug($"Warming up {3_000_000} cycles...");

                    foreach (var res in Enumerable.Range(0, warmupCycles).Select(i => testIteration(targetTps)))
                    {
                        log.Debug($"warming up ({res.Count} hiccups)");
                    }

                    log.Debug("Warmup done, starting tests");
                    foreach (var res in Enumerable.Range(0, 10000).Select(i => testIteration(targetTps)))
                    {
                        if (res.Count == 0)
                        {
                            log.Debug("no hiccups");
                        }
                        else
                        {
                            log.Debug($"------------------ {res.Count} hiccups -------------------");
                            foreach (var (timestamp, delay) in res)
                            {
                                log.Debug($"{timestamp.ToLocalTime()}: {delay / 1000}µs");
                            }
                        }
                    }

                    return null;
                });
            }
        }

    }
}
