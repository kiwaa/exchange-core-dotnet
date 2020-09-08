using Disruptor;
using Exchange.Core.Common;
using Exchange.Core.Common.Api;
using Exchange.Core.Common.Api.Binary;
using Exchange.Core.Common.Api.Reports;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Orderbook;
using Exchange.Core.Processors;
using Exchange.Core.Utils;
using log4net;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Exchange.Core
{
    public class ExchangeApi
    {
        private static ILog log = LogManager.GetLogger(typeof(ExchangeApi));
        public static readonly int LONGS_PER_MESSAGE = 5;

        // promises cache (TODO can be changed to queue)
        private ConcurrentDictionary<long, Action<OrderCommand>> promises = new ConcurrentDictionary<long, Action<OrderCommand>>();

        //internal Task<CommandResultCode> submitBinaryDataAsync(BatchAddSymbolsCommand batchAddSymbolsCommand)
        //{
        //    throw new NotImplementedException();
        //}

        //internal Task<CommandResultCode> submitCommandAsync(object p)
        //{
        //    throw new NotImplementedException();
        //}

        //internal Task<L2MarketData> requestOrderBookAsync(int symbolXbtLtc, int v)
        //{
        //    throw new NotImplementedException();
        //}

        //internal Task<R> processReport<Q,R>(Q query, int v) 
        //    where Q : ReportQuery<R> 
        //    where R : ReportResult
        //{
        //    throw new NotImplementedException();
        //}
        private RingBuffer<OrderCommand> ringBuffer;
        private LZ4Compressor lZ4Compressor;

        public ExchangeApi(RingBuffer<OrderCommand> ringBuffer, LZ4Compressor lZ4Compressor)
        {
            this.ringBuffer = ringBuffer;
            this.lZ4Compressor = lZ4Compressor;
        }

        internal void processResult(long seq, OrderCommand cmd)
        {
            if (promises.TryRemove(seq, out Action<OrderCommand> consumer))
            {
                consumer(cmd);
            }
        }

        public Task<CommandResultCode> submitCommandAsync(ApiCommand cmd)
        {
            //log.debug("{}", cmd);

            if (cmd is ApiMoveOrder)
            {
                return submitCommandAsync(MoveOrderTranslator.Instance, (ApiMoveOrder)cmd);
            }
            //else if (cmd is ApiPlaceOrder)
            //{
            //    return submitCommandAsync(NEW_ORDER_TRANSLATOR, (ApiPlaceOrder)cmd);
            //}
            //else if (cmd is ApiCancelOrder)
            //{
            //    return submitCommandAsync(CANCEL_ORDER_TRANSLATOR, (ApiCancelOrder)cmd);
            //}
            //else if (cmd is ApiReduceOrder)
            //{
            //    return submitCommandAsync(REDUCE_ORDER_TRANSLATOR, (ApiReduceOrder)cmd);
            //}
            //else if (cmd is ApiOrderBookRequest)
            //{
            //    return submitCommandAsync(ORDER_BOOK_REQUEST_TRANSLATOR, (ApiOrderBookRequest)cmd);
            //}
            else if (cmd is ApiAddUser)
            {
                return submitCommandAsync(AddUserTranslator.Instance, (ApiAddUser)cmd);
            }
            else if (cmd is ApiAdjustUserBalance)
            {
                return submitCommandAsync(AdjustUserBalanceTranslator.Instance, (ApiAdjustUserBalance)cmd);
            }
            //else if (cmd is ApiResumeUser)
            //{
            //    return submitCommandAsync(RESUME_USER_TRANSLATOR, (ApiResumeUser)cmd);
            //}
            //else if (cmd is ApiSuspendUser)
            //{
            //    return submitCommandAsync(SUSPEND_USER_TRANSLATOR, (ApiSuspendUser)cmd);
            //}
            //else if (cmd is ApiBinaryDataCommand)
            //{
            //    return submitBinaryDataAsync(((ApiBinaryDataCommand)cmd).data);
            //}
            //else if (cmd is ApiPersistState)
            //{
            //    return submitPersistCommandAsync((ApiPersistState)cmd);
            //}
            //else if (cmd is ApiReset)
            //{
            //    return submitCommandAsync(RESET_TRANSLATOR, (ApiReset)cmd);
            //}
            //else if (cmd is ApiNop)
            //{
            //    return submitCommandAsync(NOP_TRANSLATOR, (ApiNop)cmd);
            //}
            else
            {
                throw new InvalidOperationException("Unsupported command type: " + cmd.GetType().Name);
            }
        }

        public Task<CommandResultCode> submitBinaryDataAsync(IBinaryDataCommand data)
        {
            var future = new TaskCompletionSource<CommandResultCode>();

            publishBinaryData(
                    OrderCommandType.BINARY_DATA_COMMAND,
                    data,
                    data.getBinaryCommandTypeCode(),
                    (int)DateTime.UtcNow.Ticks * 100, // can be any value because sequence is used for result identification, not transferId
                    0L,
                    seq => promises.AddOrUpdate(seq,
                                orderCommand => future.SetResult(orderCommand.ResultCode),
                                (s, a1) => orderCommand => future.SetResult(orderCommand.ResultCode)));

            return future.Task;
        }

        public Task<OrderCommand> submitCommandAsyncFullResponse(ApiCommand cmd)
        {
            if (cmd is ApiMoveOrder)
            {
                return submitCommandAsyncFullResponse(MoveOrderTranslator.Instance, (ApiMoveOrder)cmd);
            }
            else if (cmd is ApiPlaceOrder)
            {
                return submitCommandAsyncFullResponse(NewOrderTranslator.Instance, (ApiPlaceOrder)cmd);
                //} else if (cmd is ApiCancelOrder) {
                //    return submitCommandAsyncFullResponse(CANCEL_ORDER_TRANSLATOR, (ApiCancelOrder)cmd);
                //} else if (cmd is ApiReduceOrder) {
                //    return submitCommandAsyncFullResponse(REDUCE_ORDER_TRANSLATOR, (ApiReduceOrder)cmd);
                //} else if (cmd is ApiOrderBookRequest) {
                //    return submitCommandAsyncFullResponse(ORDER_BOOK_REQUEST_TRANSLATOR, (ApiOrderBookRequest)cmd);
                //} else if (cmd is ApiAddUser) {
                //    return submitCommandAsyncFullResponse(ADD_USER_TRANSLATOR, (ApiAddUser)cmd);
                //} else if (cmd is ApiAdjustUserBalance) {
                //    return submitCommandAsyncFullResponse(ADJUST_USER_BALANCE_TRANSLATOR, (ApiAdjustUserBalance)cmd);
                //} else if (cmd is ApiResumeUser) {
                //    return submitCommandAsyncFullResponse(RESUME_USER_TRANSLATOR, (ApiResumeUser)cmd);
                //} else if (cmd is ApiSuspendUser) {
                //    return submitCommandAsyncFullResponse(SUSPEND_USER_TRANSLATOR, (ApiSuspendUser)cmd);
                //} else if (cmd is ApiReset) {
                //    return submitCommandAsyncFullResponse(RESET_TRANSLATOR, (ApiReset)cmd);
                //} else if (cmd is ApiNop) {
                //    return submitCommandAsyncFullResponse(NOP_TRANSLATOR, (ApiNop)cmd);
            }
            else
            {
                throw new InvalidOperationException("Unsupported command type: " + cmd.GetType().Name);
            }
        }

        public Task<L2MarketData> requestOrderBookAsync(int symbolId, int depth)
        {
            TaskCompletionSource<L2MarketData> future = new TaskCompletionSource<L2MarketData>();

            using (var scope = ringBuffer.PublishEvent())
            {
                var cmd = scope.Event();
                var seq = scope.Sequence;

                cmd.Command = OrderCommandType.ORDER_BOOK_REQUEST;
                cmd.OrderId = -1;
                cmd.Symbol = symbolId;
                cmd.Uid = -1;
                cmd.Size = depth;
                cmd.Timestamp = DateTime.UtcNow.Ticks;
                cmd.ResultCode = CommandResultCode.NEW;

                //                promises.put(seq, cmd1->future.complete(cmd1.marketData));

                promises.AddOrUpdate(seq,
                    orderCommand => future.SetResult(orderCommand.MarketData),
                    (s, a1) => orderCommand => future.SetResult(orderCommand.MarketData));
            }

            //ringBuffer.publishEvent(((cmd, seq)-> {
            //    cmd.command = OrderCommandType.ORDER_BOOK_REQUEST;
            //    cmd.orderId = -1;
            //    cmd.symbol = symbolId;
            //    cmd.uid = -1;
            //    cmd.size = depth;
            //    cmd.timestamp = System.currentTimeMillis();
            //    cmd.resultCode = CommandResultCode.NEW;

            //    promises.put(seq, cmd1->future.complete(cmd1.marketData));
            //}));

            return future.Task;
        }


        public Task<R> submitQueryAsync<T, R>(
                IReportQuery<T> data,
                int transferId,
                Func<OrderCommand, R> translator) where T : IReportResult
        {
            TaskCompletionSource<R> future = new TaskCompletionSource<R>();

            publishQuery(
                    new ApiReportQuery<T>() { query = data, transferId = transferId },
                    seq => promises.AddOrUpdate(seq,
                            orderCommand => future.SetResult(translator(orderCommand)),
                            (s, a1) => orderCommand => future.SetResult(translator(orderCommand))));

            return future.Task;
        }

        public Task<T> processReport<T>(IReportQuery<T> query, int transferId) where T : IReportResult
        {
            return submitQueryAsync(
                    query,
                    transferId,
                    cmd => query.createResult(
                            OrderBookEventsHelper.deserializeEvents(cmd).Values.AsParallel().Select(x => x.Bytes)));
        }

        public void publishQuery<T>(ApiReportQuery<T> apiCmd, Action<long> endSeqConsumer) where T : IReportResult
        {
            publishBinaryData(
                    OrderCommandType.BINARY_DATA_QUERY,
                    apiCmd.query,
                    apiCmd.query.getReportTypeCode(),
                    apiCmd.transferId,
                    apiCmd.timestamp,
                    endSeqConsumer);
        }

        public void publishBinaryData(ApiBinaryDataCommand apiCmd, Action<long> endSeqConsumer)
        {
            publishBinaryData(
                    OrderCommandType.BINARY_DATA_COMMAND,
                    apiCmd.Data,
                    apiCmd.Data.getBinaryCommandTypeCode(),
                    apiCmd.TransferId,
                    apiCmd.Timestamp,
                    endSeqConsumer);
        }


        private void publishBinaryData(OrderCommandType cmdType,
                                       IWriteBytesMarshallable data,
                                       int dataTypeCode,
                                       int transferId,
                                       long timestamp,
                                       Action<long> endSeqConsumer)
        {

            long[] longsArrayData = SerializationUtils.bytesToLongArrayLz4(
                    null,
                    BinaryCommandsProcessor.serializeObject(data, dataTypeCode),
                    LONGS_PER_MESSAGE);

            int totalNumMessagesToClaim = longsArrayData.Length / LONGS_PER_MESSAGE;

            //        log.debug("longsArrayData[{}] n={}", longsArrayData.length, totalNumMessagesToClaim);

            // max fragment size is quarter of ring buffer
            int batchSize = ringBuffer.BufferSize / 4;

            int offset = 0;
            bool isLastFragment = false;
            int fragmentSize = batchSize;

            do
            {

                if (offset + batchSize >= totalNumMessagesToClaim)
                {
                    fragmentSize = totalNumMessagesToClaim - offset;
                    isLastFragment = true;
                }

                publishBinaryMessageFragment(cmdType, transferId, timestamp, endSeqConsumer, longsArrayData, fragmentSize, offset, isLastFragment);

                offset += batchSize;

            } while (!isLastFragment);

        }


        private void publishBinaryMessageFragment(OrderCommandType cmdType,
                                                  int transferId,
                                                  long timestamp,
                                                  Action<long> endSeqConsumer,
                                                  long[] longsArrayData,
                                                  int fragmentSize,
                                                  int offset,
                                                  bool isLastFragment)
        {

            long highSeq = ringBuffer.Next(fragmentSize);
            long lowSeq = highSeq - fragmentSize + 1;

            //        log.debug("  offset*longsPerMessage={} longsArrayData[{}] n={} seq={}..{} lastFragment={} fragmentSize={}",
            //                offset * LONGS_PER_MESSAGE, longsArrayData.length, fragmentSize, lowSeq, highSeq, isLastFragment, fragmentSize);

            try
            {
                int ptr = offset * LONGS_PER_MESSAGE;
                for (long seq = lowSeq; seq <= highSeq; seq++)
                {

                    OrderCommand cmd = ringBuffer[seq];
                    cmd.Command = cmdType;
                    cmd.UserCookie = transferId;
                    cmd.Symbol = (isLastFragment && seq == highSeq) ? -1 : 0;

                    cmd.OrderId = longsArrayData[ptr];
                    cmd.Price = longsArrayData[ptr + 1];
                    cmd.ReserveBidPrice = longsArrayData[ptr + 2];
                    cmd.Size = longsArrayData[ptr + 3];
                    cmd.Uid = longsArrayData[ptr + 4];

                    cmd.Timestamp = timestamp;
                    cmd.ResultCode = CommandResultCode.NEW;

                    //                log.debug("ORIG {}", String.format("f=%d word0=%X word1=%X word2=%X word3=%X word4=%X",
                    //                cmd.symbol, longArray[i], longArray[i + 1], longArray[i + 2], longArray[i + 3], longArray[i + 4]));

                    //                log.debug("seq={} cmd.size={} data={}", seq, cmd.size, cmd.price);

                    ptr += LONGS_PER_MESSAGE;
                }
            }
            catch (Exception ex)
            {
                log.Error("Binary commands processing exception: ", ex);

            }
            finally
            {
                if (isLastFragment)
                {
                    // report last sequence before actually publishing data
                    endSeqConsumer(highSeq);
                }
                ringBuffer.Publish(lowSeq, highSeq);
            }
        }


        private Task<CommandResultCode> submitCommandAsync<T>(IEventTranslatorOneArg<OrderCommand, T> translator, T apiCommand) where T : ApiCommand
        {
            return submitCommandAsync(translator, apiCommand, c => c.ResultCode);
        }
        private Task<OrderCommand> submitCommandAsyncFullResponse<T>(IEventTranslatorOneArg<OrderCommand, T> translator, T apiCommand) where T : ApiCommand
        {
            return submitCommandAsync(translator, apiCommand, x => x);
        }


        private Task<R> submitCommandAsync<T, R>(IEventTranslatorOneArg<OrderCommand, T> translator,
                                                                                  T apiCommand,
                                                                                  Func<OrderCommand, R> responseTranslator) where T : ApiCommand
        {
            TaskCompletionSource<R> future = new TaskCompletionSource<R>();

            using (var scope = ringBuffer.PublishEvent())
            {
                var e = scope.Event();
                var seq = scope.Sequence;
                translator.TranslateTo(e, seq, apiCommand);
                promises.AddOrUpdate(seq,
                    orderCommand => future.SetResult(responseTranslator(orderCommand)),
                    (s, a1) => orderCommand => future.SetResult(responseTranslator(orderCommand)));
            }
            //ringBuffer.PublishEvent(
            //        (cmd, seq, apiCmd)=> {
            //},
            //    apiCommand);

            return future.Task;
        }

        private class NewOrderTranslator : IEventTranslatorOneArg<OrderCommand, ApiPlaceOrder>
        {
            public static readonly NewOrderTranslator Instance = new NewOrderTranslator();
            public void TranslateTo(OrderCommand cmd, long seq, ApiPlaceOrder api)
            {
                cmd.Command = OrderCommandType.PLACE_ORDER;
                cmd.Price = api.Price;
                cmd.ReserveBidPrice = api.ReservePrice;
                cmd.Size = api.Size;
                cmd.OrderId = api.OrderId;
                cmd.Timestamp = api.Timestamp;
                cmd.Action = api.Action;
                cmd.OrderType = api.OrderType;
                cmd.Symbol = api.Symbol;
                cmd.Uid = api.Uid;
                cmd.UserCookie = api.UserCookie;
                cmd.ResultCode = CommandResultCode.NEW;
            }
        };

        private class MoveOrderTranslator : IEventTranslatorOneArg<OrderCommand, ApiMoveOrder>
        {
            public static readonly MoveOrderTranslator Instance = new MoveOrderTranslator();
            public void TranslateTo(OrderCommand cmd, long seq, ApiMoveOrder api)
            {
                cmd.Command = OrderCommandType.MOVE_ORDER;
                cmd.Price = api.NewPrice;
                cmd.OrderId = api.OrderId;
                cmd.Symbol = api.Symbol;
                cmd.Uid = api.Uid;
                cmd.Timestamp = api.Timestamp;
                cmd.ResultCode = CommandResultCode.NEW;
            }
        };

        private class AddUserTranslator : IEventTranslatorOneArg<OrderCommand, ApiAddUser>
        {
            public static readonly AddUserTranslator Instance = new AddUserTranslator();
            public void TranslateTo(OrderCommand cmd, long seq, ApiAddUser api)
            {
                cmd.Command = OrderCommandType.ADD_USER;
                cmd.Uid = api.Uid;
                cmd.Timestamp = api.Timestamp;
                cmd.ResultCode = CommandResultCode.NEW;

            }
        };

        private class AdjustUserBalanceTranslator : IEventTranslatorOneArg<OrderCommand, ApiAdjustUserBalance>
        {
            public static readonly AdjustUserBalanceTranslator Instance = new AdjustUserBalanceTranslator();
            public void TranslateTo(OrderCommand cmd, long seq, ApiAdjustUserBalance api)
            {
                cmd.Command = OrderCommandType.BALANCE_ADJUSTMENT;
                cmd.OrderId = api.TransactionId;
                cmd.Symbol = api.Currency;
                cmd.Uid = api.Uid;
                cmd.Price = api.Amount;
                cmd.OrderType = (OrderType)api.AdjustmentType;
                cmd.Timestamp = api.Timestamp;
                cmd.ResultCode = CommandResultCode.NEW;

            }
        };

    }



    //private static final EventTranslatorOneArg<OrderCommand, ApiCancelOrder> CANCEL_ORDER_TRANSLATOR = (cmd, seq, api)-> {
    //    cmd.command = OrderCommandType.CANCEL_ORDER;
    //    cmd.orderId = api.orderId;
    //    cmd.symbol = api.symbol;
    //    cmd.uid = api.uid;
    //    cmd.timestamp = api.timestamp;
    //    cmd.resultCode = CommandResultCode.NEW;
    //};

    //private static final EventTranslatorOneArg<OrderCommand, ApiReduceOrder> REDUCE_ORDER_TRANSLATOR = (cmd, seq, api)-> {
    //    cmd.command = OrderCommandType.REDUCE_ORDER;
    //    cmd.orderId = api.orderId;
    //    cmd.symbol = api.symbol;
    //    cmd.uid = api.uid;
    //    cmd.size = api.reduceSize;
    //    cmd.timestamp = api.timestamp;
    //    cmd.resultCode = CommandResultCode.NEW;
    //};

    //private static final EventTranslatorOneArg<OrderCommand, ApiOrderBookRequest> ORDER_BOOK_REQUEST_TRANSLATOR = (cmd, seq, api)-> {
    //    cmd.command = OrderCommandType.ORDER_BOOK_REQUEST;
    //    cmd.symbol = api.symbol;
    //    cmd.size = api.size;
    //    cmd.timestamp = api.timestamp;
    //    cmd.resultCode = CommandResultCode.NEW;
    //};


    //private static final EventTranslatorOneArg<OrderCommand, ApiSuspendUser> SUSPEND_USER_TRANSLATOR = (cmd, seq, api)-> {
    //    cmd.command = OrderCommandType.SUSPEND_USER;
    //    cmd.uid = api.uid;
    //    cmd.timestamp = api.timestamp;
    //    cmd.resultCode = CommandResultCode.NEW;
    //};

    //private static final EventTranslatorOneArg<OrderCommand, ApiResumeUser> RESUME_USER_TRANSLATOR = (cmd, seq, api)-> {
    //    cmd.command = OrderCommandType.RESUME_USER;
    //    cmd.uid = api.uid;
    //    cmd.timestamp = api.timestamp;
    //    cmd.resultCode = CommandResultCode.NEW;
    //};

    //private static final EventTranslatorOneArg<OrderCommand, ApiReset> RESET_TRANSLATOR = (cmd, seq, api)-> {
    //    cmd.command = OrderCommandType.RESET;
    //    cmd.timestamp = api.timestamp;
    //    cmd.resultCode = CommandResultCode.NEW;
    //};

    //private static final EventTranslatorOneArg<OrderCommand, ApiNop> NOP_TRANSLATOR = (cmd, seq, api)-> {
    //    cmd.command = OrderCommandType.NOP;
    //    cmd.timestamp = api.timestamp;
    //    cmd.resultCode = CommandResultCode.NEW;
    //};

    // Mock
    public class LZ4Compressor
    {
    }
}