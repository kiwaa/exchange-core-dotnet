using Exchange.Core.Utils;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common
{
    public sealed class UserProfile : IWriteBytesMarshallable, IStateHash
    {

        public long uid { get; set; }

        // symbol -> margin position records
        // TODO initialize lazily (only needed if margin trading allowed)
        public Dictionary<int, SymbolPositionRecord> positions { get; set; }

        // protects from double adjustment
        public long adjustmentsCounter { get; set; }

        // currency accounts
        // currency -> balance
        public Dictionary<int, long> accounts { get; set; }

        public UserStatus userStatus { get; set; }

        public UserProfile(long uid, UserStatus userStatus)
        {
            //log.debug("New {}", uid);
            this.uid = uid;
            this.positions = new Dictionary<int, SymbolPositionRecord>();
            this.adjustmentsCounter = 0L;
            this.accounts = new Dictionary<int, long>();
            this.userStatus = userStatus;
        }

        public UserProfile(IBytesIn bytesIn)
        {

            this.uid = bytesIn.readLong();

            // positions
            this.positions = SerializationUtils.readIntHashMap(bytesIn, b => new SymbolPositionRecord(uid, b));

            // adjustmentsCounter
            this.adjustmentsCounter = bytesIn.readLong();

            // account balances
            this.accounts = SerializationUtils.readIntLongHashMap(bytesIn);

            // suspended
            this.userStatus = (UserStatus)bytesIn.readByte();
        }

        public SymbolPositionRecord getPositionRecordOrThrowEx(int symbol)
        {
            if (!positions.TryGetValue(symbol, out SymbolPositionRecord record))
            {
                throw new InvalidOperationException("not found position for symbol " + symbol);
            }
            return record;
        }

        public void writeMarshallable(IBytesOut bytes)
        {

            bytes.writeLong(uid);

            // positions
            SerializationUtils.marshallIntHashMap(positions, bytes);

            // adjustmentsCounter
            bytes.writeLong(adjustmentsCounter);

            // account balances
            SerializationUtils.marshallIntLongHashMap(accounts, bytes);

            // suspended
            bytes.writeByte((sbyte)userStatus);
        }


        public String toString()
        {
            return "UserProfile{" +
                    "uid=" + uid +
                    ", positions=" + positions.Count +
                    ", accounts=" + accounts +
                    ", adjustmentsCounter=" + adjustmentsCounter +
                    ", userStatus=" + userStatus +
                    '}';
        }

        public int stateHash()
        {
            return (int)(97 * uid +
                        997 * HashingUtils.stateHash(positions) +
                        9997 * adjustmentsCounter +
                        99997 * accounts.GetHashCode() +
                        999997 * userStatus.GetHashCode());
        }
    }
}
