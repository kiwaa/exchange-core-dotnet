using Exchange.Core.Common;
using Exchange.Core.Common.Api.Binary;
using Exchange.Core.Common.Api.Reports;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Orderbook;
using Exchange.Core.Utils;
using log4net;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Processors
{
    /**
 * Stateful Binary Commands Processor
 * <p>
 * Has incoming data buffer
 * Can receive events in arbitrary order and duplicates - at-least-once-delivery compatible.
 */
    public sealed class BinaryCommandsProcessor : IStateHash, IWriteBytesMarshallable
    {
        private static ILog log = LogManager.GetLogger(typeof(BinaryCommandsProcessor));

        // TODO connect object pool

        // transactionId -> TransferRecord (long array + bitset)
        private readonly Dictionary<long, TransferRecord> incomingData;

        // TODO improve type (Object is not ok)
        private readonly Action<IBinaryDataCommand> completeMessagesHandler;

        private readonly IReportQueriesHandler reportQueriesHandler;

        private readonly OrderBookEventsHelper eventsHelper;

        private readonly ReportsQueriesConfiguration queriesConfiguration;

        private readonly int section;

        public BinaryCommandsProcessor(Action<IBinaryDataCommand> completeMessagesHandler,
                                       IReportQueriesHandler reportQueriesHandler,
                                       SharedPool sharedPool,
                                       ReportsQueriesConfiguration queriesConfiguration,
                                       int section)
        {
            this.completeMessagesHandler = completeMessagesHandler;
            this.reportQueriesHandler = reportQueriesHandler;
            this.incomingData = new Dictionary<long, TransferRecord>();
            this.eventsHelper = new OrderBookEventsHelper(sharedPool.getChain);
            this.queriesConfiguration = queriesConfiguration;
            this.section = section;
        }

        public BinaryCommandsProcessor(Action<IBinaryDataCommand> completeMessagesHandler,
                                       IReportQueriesHandler reportQueriesHandler,
                                       SharedPool sharedPool,
                                       ReportsQueriesConfiguration queriesConfiguration,
                                       IBytesIn bytesIn,
                                       int section)
        {
            this.completeMessagesHandler = completeMessagesHandler;
            this.reportQueriesHandler = reportQueriesHandler;
            this.incomingData = SerializationUtils.readLongHashMap(bytesIn, b => new TransferRecord(bytesIn));
            this.eventsHelper = new OrderBookEventsHelper(sharedPool.getChain);
            this.section = section;
            this.queriesConfiguration = queriesConfiguration;
        }

        public CommandResultCode acceptBinaryFrame(OrderCommand cmd)
        {

            int transferId = cmd.UserCookie;

            if (!incomingData.TryGetValue(transferId, out TransferRecord record))
            {
                int bytesLength = (int)(cmd.OrderId >> 32) & 0x7FFF_FFFF;
                int longArraySize = SerializationUtils.requiredLongArraySize(bytesLength, ExchangeApi.LONGS_PER_MESSAGE);
                //            log.debug("EXPECTED: bytesLength={} longArraySize={}", bytesLength, longArraySize);
                record = new TransferRecord(longArraySize);
                incomingData.Add(transferId, record);
            }

            record.addWord(cmd.OrderId);
            record.addWord(cmd.Price);
            record.addWord(cmd.ReserveBidPrice);
            record.addWord(cmd.Size);
            record.addWord(cmd.Uid);

            if (cmd.Symbol == -1)
            {
                // all frames received

                incomingData.Remove(transferId);

                IBytesIn bytesIn = SerializationUtils.longsLz4ToWire(record.dataArray, record.wordsTransfered).Bytes;

                if (cmd.Command == OrderCommandType.BINARY_DATA_QUERY)
                {

                    var tmp = deserializeQuery(bytesIn);
                    if (tmp != null)
                    {
                        var res = handleReport(tmp);
                        NativeBytes bytes = Bytes.allocateElasticDirect(128);
                        res.writeMarshallable(bytes);
                        MatcherTradeEvent binaryEventsChain = eventsHelper.createBinaryEventsChain(cmd.Timestamp, section, bytes);
                        UnsafeUtils.appendEventsVolatile(cmd, binaryEventsChain);
                    }
                }
                else if (cmd.Command == OrderCommandType.BINARY_DATA_COMMAND)
                {

                    //                log.debug("Unpack {} words", record.wordsTransfered);
                    IBinaryDataCommand binaryDataCommand = deserializeBinaryCommand(bytesIn);
                    //                log.debug("Succeed");
                    completeMessagesHandler(binaryDataCommand);

                }
                else
                {
                    throw new InvalidOperationException();
                }


                return CommandResultCode.SUCCESS;
            }
            else
            {
                return CommandResultCode.ACCEPTED;
            }
        }

        private IBinaryDataCommand deserializeBinaryCommand(IBytesIn bytesIn)
        {

            int classCode = bytesIn.readInt();

            if (!queriesConfiguration.BinaryCommandConstructors.TryGetValue(classCode, out Func<IBytesIn, IBinaryDataCommand> constructor))
//                Constructor <? extends BinaryDataCommand > constructor = queriesConfiguration.getBinaryCommandConstructors().get(classCode);
//            if (constructor == null)
            {
                throw new InvalidOperationException("Unknown Binary Data Command class code: " + classCode);
            }

            try
            {
                return constructor(bytesIn);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to deserialize Binary Data Command instance of class ", ex);
            }
        }

        private object deserializeQuery(IBytesIn bytesIn)
        {

            int classCode = bytesIn.readInt();

            if (!queriesConfiguration.ReportConstructors.TryGetValue(classCode, out Func<IBytesIn, object> constructor))
            //                Constructor<? extends ReportQuery<?>> constructor = queriesConfiguration.ReportConstructors[classCode];
            //                if (constructor == null)
            {
                log.Error($"Unknown Report Query class code: {classCode}");
                return null;
            }

            try
            {
                return constructor(bytesIn);

            }
            catch (Exception ex)
            {
                log.Error($"Failed to deserialize report instance of class. error: {ex.Message}");
                return null;
            }
        }

        //public static NativeBytes<Void> serializeObject(WriteBytesMarshallable data, int objectType)
        //{
        //    final NativeBytes<Void> bytes = Bytes.allocateElasticDirect(128);
        //    bytes.writeInt(objectType);
        //    data.writeMarshallable(bytes);
        //    return bytes;
        //}

        public void reset()
        {
            incomingData.Clear();
        }

        public void writeMarshallable(IBytesOut bytes)
        {

            // write symbolSpecs
            SerializationUtils.marshallLongHashMap(incomingData, bytes);
        }

        public int stateHash()
        {
            return HashingUtils.stateHash(incomingData);
        }


        private class TransferRecord : IStateHash, IWriteBytesMarshallable
        {

            public long[] dataArray { get; set; }
            public int wordsTransfered { get; set; }

            public TransferRecord(int expectedLength)
            {
                this.wordsTransfered = 0;
                this.dataArray = new long[expectedLength];
            }

            public TransferRecord(IBytesIn bytes)
            {
                wordsTransfered = bytes.readInt();
                this.dataArray = SerializationUtils.readLongArray(bytes);
            }

            public void addWord(long word)
            {

                if (wordsTransfered == dataArray.Length)
                {
                    // should never happen
                    log.Warn($"Resizing incoming transfer buffer to {dataArray.Length * 2} longs");
                    long[] newArray = new long[dataArray.Length * 2];
                    Array.Copy(dataArray, 0, newArray, 0, dataArray.Length);
                    dataArray = newArray;
                }

                dataArray[wordsTransfered++] = word;

            }

            public void writeMarshallable(IBytesOut bytes)
            {
                bytes.writeInt(wordsTransfered);
                SerializationUtils.marshallLongArray(dataArray, bytes);
            }

            public int stateHash()
            {
                return 97 * dataArray.GetHashCode() +
                       997 * wordsTransfered;
            }
        }

        private IReportResult handleReport(object reportQuery)
        {
            switch (reportQuery)
            {
                case SingleUserReportQuery surq:
                    return reportQueriesHandler.handleReport(surq);
                case TotalCurrencyBalanceReportQuery tcbrq:
                    return reportQueriesHandler.handleReport(tcbrq);
                default:
                    throw new NotImplementedException();
            }
        }

        public static NativeBytes serializeObject(IWriteBytesMarshallable data, int objectType)
        {
            NativeBytes bytes = Bytes.allocateElasticDirect(128);
            bytes.writeInt(objectType);
            data.writeMarshallable(bytes);
            return bytes;
        }
    }
}
