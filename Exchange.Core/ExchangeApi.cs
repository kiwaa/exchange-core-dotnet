using Disruptor;
using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using System;
using System.Threading.Tasks;

namespace Exchange.Core
{
    public class ExchangeApi
    {
        public static readonly int LONGS_PER_MESSAGE = 5;
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
    }
}