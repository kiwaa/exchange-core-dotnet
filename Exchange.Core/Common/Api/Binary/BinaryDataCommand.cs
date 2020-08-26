using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Api.Binary
{
    public interface BinaryDataCommand //: WriteBytesMarshallable
    {
        int getBinaryCommandTypeCode();

    }
}
