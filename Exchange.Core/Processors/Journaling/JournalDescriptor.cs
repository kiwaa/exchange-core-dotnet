using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Processors.Journaling
{
    public class JournalDescriptor
    {

        private readonly long timestampNs;
        private readonly long seqFirst;
        private long seqLast = -1; // -1 if not finished yet

        private readonly SnapshotDescriptor baseSnapshot;

        private readonly JournalDescriptor prev; // can be null
        private JournalDescriptor next = null; // can be null

    }
}
