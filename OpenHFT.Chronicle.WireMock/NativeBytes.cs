using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHFT.Chronicle.WireMock
{
    public class NativeBytes<T> : IBytesOut
    {
        public int readRemaining()
        {
            throw new NotImplementedException();
        }

        public void read(MemoryStream byteBuffer)
        {
            throw new NotImplementedException();
        }

        public IBytesOut writeLong(long askPrice)
        {
            throw new NotImplementedException();
        }

        public IBytesOut writeInt(object p)
        {
            throw new NotImplementedException();
        }

        public IBytesOut writeByte(byte direction)
        {
            throw new NotImplementedException();
        }
    }
}
