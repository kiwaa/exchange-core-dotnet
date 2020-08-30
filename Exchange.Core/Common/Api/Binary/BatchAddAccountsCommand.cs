using Exchange.Core.Utils;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Api.Binary
{
    public sealed class BatchAddAccountsCommand : IBinaryDataCommand
    {

        public Dictionary<long, Dictionary<int, long>> users { get; set; }

    public BatchAddAccountsCommand(IBytesIn bytes)
    {
        users = SerializationUtils.readLongHashMap(bytes, c => SerializationUtils.readIntLongHashMap(bytes));
    }

    //public void writeMarshallable(BytesOut bytes)
    //{
    //    SerializationUtils.marshallLongHashMap(users, SerializationUtils::marshallIntLongHashMap, bytes);
    //}

    public int getBinaryCommandTypeCode()
    {
        return (int)BinaryCommandType.ADD_ACCOUNTS;
    }
}
}
