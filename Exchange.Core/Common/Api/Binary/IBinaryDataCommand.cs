using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Api.Binary
{
    public interface IBinaryDataCommand : IWriteBytesMarshallable
    {
        int getBinaryCommandTypeCode();

    }
}
