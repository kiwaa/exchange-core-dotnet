using Exchange.Core.Common.Api.Binary;
using Exchange.Core.Common.Api.Reports;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Exchange.Core.Common.Config
{
    public sealed partial class ReportsQueriesConfiguration
    {
        public static readonly ReportsQueriesConfiguration DEFAULT = createStandardConfig();

        /**
     * Creates default reports config
     *
     * @return reports configuration
     */
        public static ReportsQueriesConfiguration createStandardConfig()
        {
            return createStandardConfig(new Dictionary<int, Type>());
        }

        /**
         * Creates reports config with additional custom reports
         *
         * @param customReports - custom reports collection
         * @return reports configuration
         */
        public static ReportsQueriesConfiguration createStandardConfig(Dictionary<int, Type> customReports)
        {

            Dictionary<int, Func<IBytesIn, object>> reportConstructors = new Dictionary<int, Func<IBytesIn, object>>();
            Dictionary<int, Func<IBytesIn, IBinaryDataCommand>> binaryCommandConstructors = new Dictionary<int, Func<IBytesIn, IBinaryDataCommand>>();

            // binary commands (not extendable)
            addBinaryCommandClass(binaryCommandConstructors, BinaryCommandType.ADD_ACCOUNTS, typeof(BatchAddAccountsCommand));
            addBinaryCommandClass(binaryCommandConstructors, BinaryCommandType.ADD_SYMBOLS, typeof(BatchAddSymbolsCommand));

            // predefined queries (extendable)
            addQueryClass(reportConstructors, (int)ReportType.STATE_HASH, typeof(StateHashReportQuery));
            addQueryClass(reportConstructors, (int)ReportType.SINGLE_USER_REPORT, typeof(SingleUserReportQuery));
            addQueryClass(reportConstructors, (int)ReportType.TOTAL_CURRENCY_BALANCE, typeof(TotalCurrencyBalanceReportQuery));

            foreach (var pair in customReports)
                addQueryClass(reportConstructors, pair.Key, pair.Value);

            return new ReportsQueriesConfiguration(
                    reportConstructors,
                    binaryCommandConstructors);
        }


        private static void addQueryClass(Dictionary<int, Func<IBytesIn, object>> reportConstructors,
                                          int reportTypeCode,
                                          Type reportQueryClass)
        {

            if (reportConstructors.TryGetValue(reportTypeCode, out Func<IBytesIn, object> ctor))
            {
                throw new InvalidOperationException("Configuration error: report type code " + reportTypeCode + " is already occupied.");
            }

            try
            {
                reportConstructors[reportTypeCode] = (bytesIn) => Activator.CreateInstance(reportQueryClass, bytesIn);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Configuration error: report class " + reportQueryClass.Name + "deserialization constructor accepting BytesIn");
            }

        }

        private static void addBinaryCommandClass(Dictionary<int, Func<IBytesIn, IBinaryDataCommand>> binaryCommandConstructors,
                                                  BinaryCommandType type,
                                                  Type binaryCommandClass)
        {
            try
            {
                binaryCommandConstructors[(int)type] = (bytesIn) => (IBinaryDataCommand)Activator.CreateInstance(binaryCommandClass, bytesIn);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("", ex);
            }
        }

        public override string ToString()
        {
            return "ReportsQueriesConfiguration{" +
                    "reportConstructors=[" + reportToString(ReportConstructors) +
                    "], binaryCommandConstructors=[" + reportToString(BinaryCommandConstructors) +
                    "]}";
        }

        private static String reportToString(Dictionary<int, Func<IBytesIn, object>> mapping)
        {
            return string.Join(", ", mapping
                    .Select(entry => String.Format("%d", entry.Key)));
        }
        private static String reportToString(Dictionary<int, Func<IBytesIn, IBinaryDataCommand>> mapping)
        {
            return string.Join(", ", mapping
                    .Select(entry => String.Format("%d", entry.Key)));
        }
    }
}