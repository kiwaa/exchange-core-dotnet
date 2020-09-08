using Exchange.Core.Collections.Utils;
using Exchange.Core.Utils;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Api.Reports
{
    public sealed partial class StateHashReportResult : IReportResult
    {

        public static readonly StateHashReportResult EMPTY = new StateHashReportResult();

        public void writeMarshallable(IBytesOut bytes)
        {
            throw new NotImplementedException();
        }

        internal static StateHashReportResult merge(IEnumerable<IBytesIn> sections)
        {
            throw new NotImplementedException();
        }
    }
}
