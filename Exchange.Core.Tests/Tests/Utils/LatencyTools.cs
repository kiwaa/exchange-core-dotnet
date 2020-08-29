using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Utils
{
    public sealed class LatencyTools
    {

        private static readonly double[] PERCENTILES = new double[] { 50, 90, 95, 99, 99.9, 99.99 };

        //public static Map<String, String> createLatencyReportFast(Histogram histogram)
        //{
        //    final Map<String, String> fmt = new LinkedHashMap<>();
        //    Arrays.stream(PERCENTILES).forEach(p->fmt.put(p + "%", formatNanos(histogram.getValueAtPercentile(p))));
        //    fmt.put("W", formatNanos(histogram.getMaxValue()));
        //    return fmt;
        //}

        public static String formatNanos(long ns)
        {
            float value = ns / 1000f;
            String timeUnit = "µs";
            if (value > 1000)
            {
                value /= 1000;
                timeUnit = "ms";
            }

            if (value > 1000)
            {
                value /= 1000;
                timeUnit = "s";
            }

            if (value < 3)
            {
                return Math.Round(value * 100) / 100f + timeUnit;
            }
            else if (value < 30)
            {
                return Math.Round(value * 10) / 10f + timeUnit;
            }
            else
            {
                return Math.Round(value) + timeUnit;
            }
        }
    }
}
