using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHFT.Chronicle.WireMock
{
    public class Wire
    {
        public IBytesIn Bytes { get; private set; }

        public static Wire Raw(byte[] vs)
        {
            return new Wire()
            {
                Bytes = new NativeBytes(vs)
            };
        }
    }
}
