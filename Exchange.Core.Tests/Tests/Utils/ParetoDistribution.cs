using System;


namespace Exchange.Core.Tests.Utils
{
    public class ParetoDistribution
    {
        private double xm;
        private double alpha;
        private Random source;

        public ParetoDistribution(Random rnd, double scale, double shape)
        {
            source = rnd;
            xm = scale;
            alpha = shape;

        }

        public double Sample()
        {
            double u = source.NextDouble();
            return xm / Math.Pow(u, 1.0 / alpha);
        }
    }
}