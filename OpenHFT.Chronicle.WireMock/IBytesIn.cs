﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHFT.Chronicle.WireMock
{
    public interface IBytesIn
    {
        int readInt();
        long readLong();
        sbyte readByte();

        bool readBool();
    }
}
