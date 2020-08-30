using Exchange.Core.Common.Config;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;

namespace Exchange.Core.Processors.Journaling
{
    public interface ISerializationProcessor
    {
        /**
     * Serialize state into a storage (disk, NAS, etc).<p>
     * Method is threadsafe - called from each module's thread upon receiving serialization command.<p>
     * Method is synchronous - returning true value only when the data was safely stored into independent storage.<p>
     *
     * @param snapshotId  - unique snapshot id
     * @param seq         - sequence of serialization
     * @param timestampNs - timestamp
     * @param type        - module (risk engine or matching engine)
     * @param instanceId  - module instance number (starting from 0 for each module type)
     * @param obj         - serialized data
     * @return true if serialization succeeded, false otherwise
     */
        bool storeData(long snapshotId,
                          long seq,
                          long timestampNs,
                          SerializedModuleType type,
                          int instanceId,
                          IWriteBytesMarshallable obj);

        /**
         * Deserialize state from a storage (disk, NAS, etc).<p>
         * Method is threadsafe - called from each module's thread on creation.<p>
         *
         * @param snapshotId - unique snapshot id
         * @param type       - module (risk engine or matching engine)
         * @param instanceId - module instance number (starting from 0)
         * @param initFunc   - creator lambda function
         * @param <T>        - module implementation class
         * @return constructed object, or throws exception
         */
        T loadData<T>(long snapshotId,
                   SerializedModuleType type,
                   int instanceId,
                   Func<IBytesIn, T> initFunc);


        /**
         * Write command into journal
         *
         * @param cmd  - command to write
         * @param dSeq - disruptor sequence
         * @param eob  - if true, journal should commit all previous data synchronously
         * @throws IOException - can throw in case of writing issue (will stop exchange core from responding)
         */
        void writeToJournal(OrderCommand cmd, long dSeq, bool eob);

        /**
         * Activate journal
         *
         * @param afterSeq - enable only after specified sequence, for lower sequences no writes to journal
         * @param api      - API reference
         */
        void enableJournaling(long afterSeq, ExchangeApi api);

        /**
         * get all available snapshots
         *
         * @return sequential map of snapshots (TODO can be a tree)
         */
        SortedDictionary<long, SnapshotDescriptor> findAllSnapshotPoints();

        /**
         * Replay journal
         *
         * @param snapshotId - snapshot id (important for tree history)
         * @param seqFrom    - starting command sequence (exclusive)
         * @param seqTo      - ending command sequence (inclusive)
         * @param api        - API reference
         */
        void replayJournalStep(long snapshotId, long seqFrom, long seqTo, ExchangeApi api);

        long replayJournalFull(InitialStateConfiguration initialStateConfiguration, ExchangeApi api);

        void replayJournalFullAndThenEnableJouraling(InitialStateConfiguration initialStateConfiguration, ExchangeApi exchangeApi);


    }
}