using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core
{
    public class DummySerializationProcessor// : ISerializationProcessor
    {
        public static readonly DummySerializationProcessor INSTANCE = new DummySerializationProcessor();
    }
}
