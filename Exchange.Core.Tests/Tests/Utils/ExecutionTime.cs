using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Utils
{
    public class ExecutionTime : IDisposable
    {
        private const long NANOS_PER_SECOND = 1000000000L;

        private readonly Action<String> executionTimeConsumer;

        private readonly long startNs = Stopwatch.GetTimestamp();
        private long endNs = 0;

        private bool _disposed = false;

        public long ResultNs => (endNs - startNs) / Stopwatch.Frequency * NANOS_PER_SECOND;
        public ExecutionTime()
        {
            this.executionTimeConsumer = s =>
            {
            };
        }

        public ExecutionTime(Action<String> executionTimeConsumer)
        {
            this.executionTimeConsumer = executionTimeConsumer;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                _disposed = true;
                endNs = Stopwatch.GetTimestamp();
            }
            executionTimeConsumer(getTimeFormatted());
        }

        public String getTimeFormatted()
        {
            return LatencyTools.formatNanos(ResultNs);
        }
    }

}
