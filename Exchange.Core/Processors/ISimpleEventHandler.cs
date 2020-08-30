using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Processors
{
    public interface ISimpleEventHandler
    {

        /**
         * Handle command with resulting data
         *
         * @param seq   - sequence number
         * @param event - event
         * @return true to forcibly publish sequence (batches)
         */
        bool onEvent(long seq, OrderCommand evnt);

    }
}
