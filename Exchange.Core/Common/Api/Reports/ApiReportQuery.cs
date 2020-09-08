using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Api.Reports
{
    public sealed class ApiReportQuery<T> where T : IReportResult
    {

        public long timestamp { get; set; }

        // transfer unique id
        // can be constant unless going to push data concurrently
        public int transferId { get; set; }

        // serializable object
        public IReportQuery<T> query { get; set; }
    }
}
