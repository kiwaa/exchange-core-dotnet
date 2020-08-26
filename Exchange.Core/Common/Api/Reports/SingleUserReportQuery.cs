namespace Exchange.Core.Tests.Examples
{
    internal class SingleUserReportQuery : ReportQuery<SingleUserReportResult>
    {
        private int v;

        public SingleUserReportQuery(int v)
        {
            this.v = v;
        }
    }
}