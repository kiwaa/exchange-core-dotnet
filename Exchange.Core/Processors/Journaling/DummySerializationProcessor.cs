using Exchange.Core.Common.Config;
using Exchange.Core.Processors.Journaling;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core
{
    public class DummySerializationProcessor : ISerializationProcessor
    {
        public static readonly DummySerializationProcessor INSTANCE = new DummySerializationProcessor();

        public void enableJournaling(long afterSeq, ExchangeApi api)
        {
            throw new NotImplementedException();
        }

        public SortedDictionary<long, SnapshotDescriptor> findAllSnapshotPoints()
        {
            throw new NotImplementedException();
        }

        public T loadData<T>(long snapshotId, SerializedModuleType type, int instanceId, Func<IBytesIn, T> initFunc)
        {
            throw new NotImplementedException();
        }

        public long replayJournalFull(InitialStateConfiguration initialStateConfiguration, ExchangeApi api)
        {
            throw new NotImplementedException();
        }

        public void replayJournalFullAndThenEnableJouraling(InitialStateConfiguration initialStateConfiguration, ExchangeApi exchangeApi)
        {
            throw new NotImplementedException();
        }

        public void replayJournalStep(long snapshotId, long seqFrom, long seqTo, ExchangeApi api)
        {
            throw new NotImplementedException();
        }

        public bool storeData(long snapshotId, long seq, long timestampNs, SerializedModuleType type, int instanceId, IWriteBytesMarshallable obj)
        {
            throw new NotImplementedException();
        }

        public void writeToJournal(OrderCommand cmd, long dSeq, bool eob)
        {
            throw new NotImplementedException();
        }
    }
}
