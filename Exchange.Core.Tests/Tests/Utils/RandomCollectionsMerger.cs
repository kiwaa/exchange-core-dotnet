//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Exchange.Core.Tests.Utils
//{
//    public class RandomCollectionsMerger
//    {

//        public static List<T> mergeCollections<T>(IEnumerable<IEnumerable<T>> chunks, long seed)
//        {
//            //JDKRandomGenerator jdkRandomGenerator = new JDKRandomGenerator(Long.hashCode(seed));
//            var rnd = new Random(seed.GetHashCode());

//            List<T> mergedResult = new List<T>();

//            // create initial weight pairs
//            List<Tuple<Spliterator<T>, double>> weightPairs = chunks
//                .Select(chunk => Tuple.Create(chunk.spliterator(), (double)chunk.Count()))
//                .ToList();

//            while (weightPairs.Count > 0))
//            {

//                EnumeratedDistribution<Spliterator< T >> ed = new EnumeratedDistribution<>(rnd, weightPairs);

//                // take random elements until face too many misses
//                int missCounter = 0;
//                while (missCounter++ < 3)
//                {
//                    Spliterator<T> sample = ed.sample();
//                    if (sample.tryAdvance(mergedResult::add))
//                    {
//                        missCounter = 0;
//                    }
//                }

//                // as empty queues leading to misses - rebuild wight pairs without them
//                weightPairs = weightPairs
//                        .Where(p => p.getFirst().estimateSize() > 0)
//                        .Select(p => Pair.create(p.getFirst(), (double)p.getFirst().estimateSize()))
//                        .ToList();

//                //            log.debug("rebuild size {}", weightPairs.size());
//            }

//            return mergedResult;
//        }
//    }

//}
