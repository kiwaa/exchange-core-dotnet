//using log4net;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Exchange.Core.Collections.Tests
//{
//    [TestFixture]
//    public class PerfLongAdaptiveRadixTreeMap
//    {

//        private static readonly ILog log = LogManager.GetLogger(typeof(PerfLongAdaptiveRadixTreeMap));

//        private enum Benchmark
//        {
//            BST_PUT,
//            BST_GET_HIT,
//            BST_REMOVE,
//            BST_FOREACH,
//            BST_FOREACH_DESC,
//            BST_HIGHER,
//            BST_LOWER,
//            ART_PUT,
//            ART_GET_HIT,
//            ART_REMOVE,
//            ART_FOREACH,
//            ART_FOREACH_DESC,
//            ART_HIGHER,
//            ART_LOWER
//        }

//        private Random rand;
//        private LongAdaptiveRadixTreeMap<long> art;
//        private SortedDictionary<long, long> bst;
//        private Dictionary<Benchmark, List<long>> times = new Dictionary<Benchmark, List<long>>();
//        private Action<Benchmark, long> benchmarkConsumer =
//                (b, t) => times.compute(b, (k, v) => v == null ? new List<>() : v).add(t);

//        private List<long> list;

//        [Test]
//        public void shouldLoadManyItems()
//        {

//            rand = new Random(1);

//            final UnaryOperator<Integer> stepFunction = i-> 1 + rand.nextInt((int)Math.min(Integer.MAX_VALUE, 1L + (Long.highestOneBit(i) >> 8)));
//            //        final UnaryOperator<Integer> stepFunction = i->1;
//            //        final UnaryOperator<Integer> stepFunction = i->1+rand.nextInt(Integer.MAX_VALUE);

//            final int forEachSize = 5000;

//            final List<Long> forEachKeysArt = new ArrayList<>(forEachSize);
//            final List<Long> forEachValuesArt = new ArrayList<>(forEachSize);
//            final LongObjConsumer<Long> forEachConsumerArt = (k, v)-> {
//                forEachKeysArt.add(k);
//                forEachValuesArt.add(v);
//            };

//            final List<Long> forEachKeysBst = new ArrayList<>(forEachSize);
//            final List<Long> forEachValuesBst = new ArrayList<>(forEachSize);
//            final BiConsumer<Long, Long> forEachConsumerBst = (k, v)-> {
//                forEachKeysBst.add(k);
//                forEachValuesBst.add(v);
//            };


//            long timeEnd = System.currentTimeMillis() + 600_000;
//            for (int iter = 0; System.currentTimeMillis() < timeEnd && iter < 3000; iter++)
//            {
//                log.debug("-------------------iteration:{} ({}s left)------------------------", iter, (timeEnd - System.currentTimeMillis()) / 1000);

//                art = new LongAdaptiveRadixTreeMap<>();
//                bst = new TreeMap<>();

//                int num = 5_000_000;
//                //            int num = 1000;
//                list = new ArrayList<>(num);
//                long j = 0;
//                log.debug("generate random numbers..");
//                long offset = 1_000_000_000L + rand.nextInt(1_000_000);
//                for (int i = 0; i < num; i++)
//                {
//                    list.add(offset + j);
//                    j += stepFunction.apply(i);
//                }
//                log.debug("shuffle..");
//                Collections.shuffle(list, rand);

//                executeInRandomOrder(()-> {
//                    log.debug("put into BST..");
//                    long t = System.nanoTime();
//                    list.forEach(x->bst.put(x, x));
//                    benchmarkConsumer.accept(Benchmark.BST_PUT, System.nanoTime() - t);
//                }, ()-> {
//                    log.debug("put into ADT..");
//                    long t = System.nanoTime();
//                    list.forEach(x->art.put(x, x));
//                    benchmarkConsumer.accept(Benchmark.ART_PUT, System.nanoTime() - t);
//                });

//            log.debug("shuffle..");
//            Collections.shuffle(list, rand);

//            executeInRandomOrder(()-> {
//                log.debug("get (hit) from BST..");
//                long sum = 0;
//                long t = System.nanoTime();
//                for (long x : list)
//                {
//                    sum += bst.get(x);
//                }
//                benchmarkConsumer.accept(Benchmark.BST_GET_HIT, System.nanoTime() - t);
//                log.debug("done ({})", sum);
//            }, ()-> {
//                log.debug("get (hit) from ADT..");
//                long sum = 0;
//                long t = System.nanoTime();
//                for (long x : list)
//                {
//                    sum += art.get(x);
//                }
//                benchmarkConsumer.accept(Benchmark.ART_GET_HIT, System.nanoTime() - t);
//                log.debug("done ({})", sum);
//            });

//            //log.debug("\n{}", art.printDiagram());

//            log.debug("validating..");
//            art.validateInternalState();
//            checkStreamsEqual(art.entriesList().stream(), bst.entrySet().stream());

//            log.debug("shuffle again..");
//            Collections.shuffle(list, rand);

//            executeInRandomOrder(()-> {
//                log.debug("higher from ART..");
//                long sum = 0;
//                long t = System.nanoTime();
//                for (long x : list)
//                {
//                    Long v = art.getHigherValue(x);
//                    sum += v == null ? 0 : v;
//                }
//                benchmarkConsumer.accept(Benchmark.ART_HIGHER, System.nanoTime() - t);
//                log.debug("done ({})", sum);
//            }, ()-> {

//                log.debug("higher from BST..");
//                long sum = 0;
//                long t = System.nanoTime();
//                for (long x : list)
//                {
//                    Map.Entry<Long, Long> entry = bst.higherEntry(x);
//                    sum += (entry != null ? entry.getValue() : 0);
//                }
//                benchmarkConsumer.accept(Benchmark.BST_HIGHER, System.nanoTime() - t);
//                log.debug("done ({})", sum);
//            });

//            executeInRandomOrder(()-> {
//                log.debug("lower from ART..");
//                long sum = 0;
//                long t = System.nanoTime();
//                for (long x : list)
//                {
//                    Long v = art.getLowerValue(x);
//                    sum += v == null ? 0 : v;
//                }
//                benchmarkConsumer.accept(Benchmark.ART_LOWER, System.nanoTime() - t);
//                log.debug("done ({})", sum);
//            }, ()-> {

//                log.debug("lower from BST..");
//                long t = System.nanoTime();
//                long sum = 0;
//                for (long x : list)
//                {
//                    Map.Entry<Long, Long> entry = bst.lowerEntry(x);
//                    sum += (entry != null ? entry.getValue() : 0);
//                }
//                benchmarkConsumer.accept(Benchmark.BST_LOWER, System.nanoTime() - t);
//                log.debug("done ({})", sum);
//            });

//            log.debug("validate getHigherValue method..");
//            for (long x : list)
//            {
//                //                log.debug("CHECK:{} {} ---------", x, String.format("%Xh", x));
//                Long v1 = art.getHigherValue(x);
//                Map.Entry<Long, Long> entry = bst.higherEntry(x);
//                Long v2 = entry != null ? entry.getValue() : null;
//                if (!Objects.equals(v1, v2))
//                {
//                    log.debug("ART  :{} {}", v1, String.format("%Xh", v1));
//                    log.debug("BST  :{} {}", v2, String.format("%Xh", v2));
//                    System.out.println(art.printDiagram());
//                    throw new IllegalStateException();
//                }

//                //                assertThat(v1, is(v2));
//            }

//            log.debug("validate getLowerValue method..");
//            for (long x : list)
//            {
//                //                log.debug("CHECK:{} {} ---------", x, String.format("%Xh", x));
//                Long v1 = art.getLowerValue(x);
//                Map.Entry<Long, Long> entry = bst.lowerEntry(x);
//                Long v2 = entry != null ? entry.getValue() : null;
//                if (!Objects.equals(v1, v2))
//                {
//                    log.debug("ART  :{} {}", v1, String.format("%Xh", v1));
//                    log.debug("BST  :{} {}", v2, String.format("%Xh", v2));
//                    System.out.println(art.printDiagram());
//                    throw new IllegalStateException();
//                }

//                //                assertThat(v1, is(v2));
//            }

//            //            log.debug("\n{}", art.printDiagram());

//            executeInRandomOrder(()-> {
//                log.debug("forEach BST...");
//                long t = System.nanoTime();
//                bst.entrySet().stream().limit(forEachSize).forEach(e->forEachConsumerBst.accept(e.getKey(), e.getValue()));
//                benchmarkConsumer.accept(Benchmark.BST_FOREACH, System.nanoTime() - t);
//            }, ()-> {
//                log.debug("forEach ADT...");
//                long t = System.nanoTime();
//                art.forEach(forEachConsumerArt, forEachSize);
//                benchmarkConsumer.accept(Benchmark.ART_FOREACH, System.nanoTime() - t);
//            });

//            //            log.debug(" forEach size {} vs {}", forEachKeysArt.size(), forEachKeysBst.size());

//            log.debug("validate forEach...");
//            assertThat(forEachKeysArt, is (forEachKeysBst));
//            assertThat(forEachValuesArt, is (forEachValuesBst));
//            forEachKeysArt.clear();
//            forEachKeysBst.clear();
//            forEachValuesArt.clear();
//            forEachValuesBst.clear();

//            executeInRandomOrder(()-> {
//                log.debug("forEachDesc BST...");
//                long t = System.nanoTime();
//                bst.descendingMap().entrySet().stream().limit(forEachSize).forEach(e->forEachConsumerBst.accept(e.getKey(), e.getValue()));
//                benchmarkConsumer.accept(Benchmark.BST_FOREACH_DESC, System.nanoTime() - t);
//            }, ()-> {
//                log.debug("forEachDesc ADT...");
//                long t = System.nanoTime();
//                art.forEachDesc(forEachConsumerArt, forEachSize);
//                benchmarkConsumer.accept(Benchmark.ART_FOREACH_DESC, System.nanoTime() - t);
//            });
//            //            log.debug(" forEach size {} vs {}", forEachKeysArt.size(), forEachKeysBst.size());

//            log.debug("validate forEachDesc...");
//            assertThat(forEachKeysArt, is (forEachKeysBst));
//            assertThat(forEachValuesArt, is (forEachValuesBst));
//            forEachKeysArt.clear();
//            forEachKeysBst.clear();
//            forEachValuesArt.clear();
//            forEachValuesBst.clear();

//            executeInRandomOrder(()-> {
//                log.debug("remove from BST..");
//                long t = System.nanoTime();
//                list.forEach(bst::remove);
//                benchmarkConsumer.accept(Benchmark.BST_REMOVE, System.nanoTime() - t);

//            }, ()-> {
//                log.debug("remove from ADT..");
//                //        list.forEach(x -> {
//                ////            log.debug("\n{}", adt.printDiagram());
//                //            adt.validateInternalState();
//                //            log.debug("REMOVING {}", x);
//                //            adt.remove(x);
//                //        });
//                long t = System.nanoTime();
//                list.forEach(art::remove);
//                benchmarkConsumer.accept(Benchmark.ART_REMOVE, System.nanoTime() - t);
//            });


//            log.debug("validating..");
//            art.validateInternalState();
//            checkStreamsEqual(art.entriesList().stream(), bst.entrySet().stream());


//            Function<Benchmark, Long> getBenchmarkNs = b->Math.round(times.get(b).stream().mapToLong(x->x).average().orElse(0));

//            long bstPutTimeNsAvg = getBenchmarkNs.apply(Benchmark.BST_PUT);
//            long artPutTimeNsAvg = getBenchmarkNs.apply(Benchmark.ART_PUT);
//            long bstGetHitTimeNsAvg = getBenchmarkNs.apply(Benchmark.BST_GET_HIT);
//            long artGetHitTimeNsAvg = getBenchmarkNs.apply(Benchmark.ART_GET_HIT);
//            long bstRemoveTimeNsAvg = getBenchmarkNs.apply(Benchmark.BST_REMOVE);
//            long artRemoveTimeNsAvg = getBenchmarkNs.apply(Benchmark.ART_REMOVE);
//            long bstForEachTimeNsAvg = getBenchmarkNs.apply(Benchmark.BST_FOREACH);
//            long artForEachTimeNsAvg = getBenchmarkNs.apply(Benchmark.ART_FOREACH);
//            long bstForEachDescTimeNsAvg = getBenchmarkNs.apply(Benchmark.BST_FOREACH_DESC);
//            long artForEachDescTimeNsAvg = getBenchmarkNs.apply(Benchmark.ART_FOREACH_DESC);
//            long bstHigherTimeNsAvg = getBenchmarkNs.apply(Benchmark.BST_HIGHER);
//            long artHigherTimeNsAvg = getBenchmarkNs.apply(Benchmark.ART_HIGHER);
//            long bstLowerTimeNsAvg = getBenchmarkNs.apply(Benchmark.BST_LOWER);
//            long artLowerTimeNsAvg = getBenchmarkNs.apply(Benchmark.ART_LOWER);

//            // remove 1/2 oldest results
//            if (iter % 2 == 1)
//            {
//                times.values().forEach(v->v.remove(0));
//            }


//            log.info("AVERAGE PUT    BST {}ms ADT {}ms ({}%)",
//                    nanoToMs(bstPutTimeNsAvg), nanoToMs(artPutTimeNsAvg), percentImprovement(bstPutTimeNsAvg, artPutTimeNsAvg));

//            log.info("AVERAGE GETHIT BST {}ms ADT {}ms ({}%)",
//                    nanoToMs(bstGetHitTimeNsAvg), nanoToMs(artGetHitTimeNsAvg), percentImprovement(bstGetHitTimeNsAvg, artGetHitTimeNsAvg));

//            log.info("AVERAGE REMOVE BST {}ms ADT {}ms ({}%)",
//                    nanoToMs(bstRemoveTimeNsAvg), nanoToMs(artRemoveTimeNsAvg), percentImprovement(bstRemoveTimeNsAvg, artRemoveTimeNsAvg));

//            log.info("AVERAGE FOREACH BST {}ms ADT {}ms ({}%)",
//                    nanoToMs(bstForEachTimeNsAvg), nanoToMs(artForEachTimeNsAvg), percentImprovement(bstForEachTimeNsAvg, artForEachTimeNsAvg));

//            log.info("AVERAGE FOREACH DESC BST {}ms ADT {}ms ({}%)",
//                    nanoToMs(bstForEachDescTimeNsAvg), nanoToMs(artForEachDescTimeNsAvg), percentImprovement(bstForEachDescTimeNsAvg, artForEachDescTimeNsAvg));

//            log.info("AVERAGE HIGHER BST {}ms ADT {}ms ({}%)",
//                    nanoToMs(bstHigherTimeNsAvg), nanoToMs(artHigherTimeNsAvg), percentImprovement(bstHigherTimeNsAvg, artHigherTimeNsAvg));

//            log.info("AVERAGE LOWER BST {}ms ADT {}ms ({}%)",
//                    nanoToMs(bstLowerTimeNsAvg), nanoToMs(artLowerTimeNsAvg), percentImprovement(bstLowerTimeNsAvg, artLowerTimeNsAvg));
//        }

//        log.info("---------------------------------------");
//    }

//    private void executeInRandomOrder(Runnable a, Runnable b)
//    {
//        if (rand.nextBoolean())
//        {
//            a.run();
//            b.run();
//        }
//        else
//        {
//            b.run();
//            a.run();
//        }
//    }


//    private static float nanoToMs(long nano)
//    {
//        return ((float)(nano / 1000)) / 1000f;
//    }

//    private static int percentImprovement(long oldTime, long newTime)
//    {
//        return (int)(100f * ((float)oldTime / (float)newTime - 1f));
//    }

//    private static <K, V> void checkStreamsEqual(final Stream<Map.Entry<K, V>> entry, final Stream<Map.Entry<K, V>> origEntry)
//    {
//        final Iterator<Map.Entry<K, V>> iter = entry.iterator();
//        final Iterator<Map.Entry<K, V>> origIter = origEntry.iterator();
//        while (iter.hasNext() && origIter.hasNext())
//        {
//            final Map.Entry<K, V> next = iter.next();
//            final Map.Entry<K, V> origNext = origIter.next();
//            if (!next.getKey().equals(origNext.getKey()))
//            {
//                throw new IllegalStateException(String.format("unexpected key: %s  (expected %s)", next.getKey(), origNext.getKey()));
//            }
//            if (!next.getValue().equals(origNext.getValue()))
//            {
//                throw new IllegalStateException(String.format("unexpected value: %s  (expected %s)", next.getValue(), origNext.getValue()));
//            }
//        }
//        if (iter.hasNext() || origIter.hasNext())
//        {
//            throw new IllegalStateException("different size");
//        }
//    }


//}
//}
