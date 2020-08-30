using System;

namespace Exchange.Core.Common.Config
{
    public sealed partial class InitialStateConfiguration
    {
        public static InitialStateConfiguration DEFAULT = cleanStart("MY_EXCHANGE");
        public static InitialStateConfiguration CLEAN_TEST = InitialStateConfiguration.cleanStart("EC0");

        public bool fromSnapshot()
        {
            return SnapshotId != 0;
        }

        /**
         * Clean start configuration
         *
         * @param exchangeId Exchange ID
         * @return clean start configuration without journaling.
         */
        public static InitialStateConfiguration cleanStart(string exchangeId)
        {

            return Builder()
                    .exchangeId(exchangeId)
                    .snapshotId(0)
                    .build();
        }

        /**
         * Clean start configuration with journaling on.
         *
         * @param exchangeId Exchange ID
         * @return clean start configuration with journaling on.
         */
        public static InitialStateConfiguration cleanStartJournaling(String exchangeId)
        {

            return Builder()
                    .exchangeId(exchangeId)
                    .snapshotId(0)
                    .snapshotBaseSeq(0)
                    .build();
        }

        /**
         * Configuration that loads from snapshot, without journal replay with journaling off.
         *
         * @param exchangeId Exchange ID
         * @param snapshotId snapshot ID
         * @param baseSeq    bas seq
         * @return configuration that loads from snapshot, without journal replay with journaling off.
         */
        public static InitialStateConfiguration fromSnapshotOnly(String exchangeId, long snapshotId, long baseSeq)
        {

            return Builder()
                    .exchangeId(exchangeId)
                    .snapshotId(snapshotId)
                    .snapshotBaseSeq(baseSeq)
                    .build();
        }


        /**
         * Configuration that load exchange from last known state including journal replay till last known start. Journal is enabled.
         *
         * @param exchangeId Exchange ID
         * @param snapshotId snapshot ID
         * @param baseSeq    bas seq
         * @return configuration that load exchange from last known state including journal replay till last known start. Journal is enabled.
         * TODO how to recreate from the next journal section recorded after the first recovery?
         */
        public static InitialStateConfiguration lastKnownStateFromJournal(String exchangeId, long snapshotId, long baseSeq)
        {

            return Builder()
                    .exchangeId(exchangeId)
                    .snapshotId(snapshotId)
                    .snapshotBaseSeq(baseSeq)
                    .journalTimestampNs(long.MaxValue)
                    .build();
        }
    }
}