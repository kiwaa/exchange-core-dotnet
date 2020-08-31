using Disruptor;
using Exchange.Core.Common.Cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Processors
{
    public sealed class ResultsHandler : IEventHandler<OrderCommand>
    {

        private readonly Action<OrderCommand, long> resultsConsumer;

        private bool processingEnabled = true;

        public ResultsHandler(Action<OrderCommand, long> resultsConsumer)
        {
            this.resultsConsumer = resultsConsumer;
        }

        public void OnEvent(OrderCommand cmd, long sequence, bool endOfBatch)
        {
            if (cmd.Command == OrderCommandType.GROUPING_CONTROL)
            {
                processingEnabled = cmd.OrderId == 1;
            }

            if (processingEnabled)
            {
                resultsConsumer(cmd, sequence);
            }
        }
    }

}
