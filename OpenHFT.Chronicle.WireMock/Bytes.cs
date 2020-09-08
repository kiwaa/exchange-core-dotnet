using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHFT.Chronicle.WireMock
{
    public class Bytes
    {
        public static NativeBytes allocateElasticDirect(int size)
        {
            return new NativeBytes(size);
        }
    }
}
