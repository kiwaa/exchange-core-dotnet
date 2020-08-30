using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Processors.Journaling
{

    public class SnapshotDescriptor : IComparable<SnapshotDescriptor>
    {

        private readonly long snapshotId; // 0 means empty snapshot (clean start)

        // sequence when snapshot was made
        private readonly long seq;
        private readonly long timestampNs;

        // next and previous snapshots
        private readonly SnapshotDescriptor prev;
    private SnapshotDescriptor next = null; // TODO can be a list

        private readonly int numMatchingEngines;
        private readonly int numRiskEngines;

        // all journals based on this snapshot
        // mapping: startingSeq -> JournalDescriptor
        private readonly SortedDictionary<long, JournalDescriptor> journals = new SortedDictionary<long, JournalDescriptor>();

        public SnapshotDescriptor(long snapshotId, long seq, long timestampNs, SnapshotDescriptor next, int numMatchingEngines, int numRiskEngines)
        {
            this.snapshotId = snapshotId;
            this.seq = seq;
            this.timestampNs = timestampNs;
            this.next = next;
            this.numMatchingEngines = numMatchingEngines;
            this.numRiskEngines = numRiskEngines;
        }

        /**
         * Create initial empty snapshot descriptor
         *
         * @param initialNumME - number of matching engine instances
         * @param initialNumRE - number of risk engine instances
         * @return new instance
         */
        public static SnapshotDescriptor createEmpty(int initialNumME, int initialNumRE)
        {
            return new SnapshotDescriptor(0, 0, 0, null, initialNumME, initialNumRE);
        }

        public SnapshotDescriptor createNext(long snapshotId, long seq, long timestampNs)
        {
            return new SnapshotDescriptor(snapshotId, seq, timestampNs, this, numMatchingEngines, numRiskEngines);
        }

        public int CompareTo(SnapshotDescriptor o)
        {
            return this.seq.CompareTo(o.seq);
        }
    }
}
