using Exchange.Core.Common;
using System.Collections.Generic;

namespace Exchange.Core.Common.Api.Binary
{
    internal class BatchAddSymbolsCommand : IBinaryDataCommand
    {
        public Dictionary<int, CoreSymbolSpecification> symbols { get; }

        public BatchAddSymbolsCommand(CoreSymbolSpecification symbol)
        {
            symbols = new Dictionary<int, CoreSymbolSpecification>();
            symbols.Add(symbol.SymbolId, symbol);
        }

        public BatchAddSymbolsCommand(IEnumerable<CoreSymbolSpecification> collection)
        {
            symbols = new Dictionary<int, CoreSymbolSpecification>();
            foreach (var s in collection)
                symbols[s.SymbolId] = s;
        }


        //public BatchAddSymbolsCommand(IBytesIn bytes)
        //{
        //    symbols = SerializationUtils.readIntHashMap(bytes, CoreSymbolSpecification::new);
        //}

    //public void writeMarshallable(BytesOut bytes)
    //    {
    //        SerializationUtils.marshallIntHashMap(symbols, bytes);
    //    }

        public int getBinaryCommandTypeCode()
        {
            return (int)BinaryCommandType.ADD_SYMBOLS;
        }
    }
}