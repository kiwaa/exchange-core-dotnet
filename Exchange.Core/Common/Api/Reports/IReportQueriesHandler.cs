using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Api.Reports
{
    public interface IReportQueriesHandler
    {
        R handleReport<R>(IReportQuery<R> reportQuery) where R : IReportResult;
    }
}
