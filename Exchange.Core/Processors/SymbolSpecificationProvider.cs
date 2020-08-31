using Exchange.Core.Common;
using Exchange.Core.Utils;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Processors
{
    public sealed class SymbolSpecificationProvider : IWriteBytesMarshallable, IStateHash
    {

        // symbol->specs
        private readonly Dictionary<int, CoreSymbolSpecification> symbolSpecs;

        public SymbolSpecificationProvider()
        {
            this.symbolSpecs = new Dictionary<int, CoreSymbolSpecification>();
        }

        public SymbolSpecificationProvider(IBytesIn bytes)
        {
            this.symbolSpecs = SerializationUtils.readIntHashMap(bytes, bytesIn => new CoreSymbolSpecification(bytesIn));
        }


        public bool addSymbol(CoreSymbolSpecification symbolSpecification)
        {
            if (getSymbolSpecification(symbolSpecification.SymbolId) != null)
            {
                return false; // CommandResultCode.SYMBOL_MGMT_SYMBOL_ALREADY_EXISTS;
            }
            else
            {
                registerSymbol(symbolSpecification.SymbolId, symbolSpecification);
                return true;
            }
        }

        /**
         * Get symbol specification
         *
         * @param symbol - symbol code
         * @return symbol specification
         */
        public CoreSymbolSpecification getSymbolSpecification(int symbol)
        {
            if (symbolSpecs.TryGetValue(symbol, out CoreSymbolSpecification result))
                return result;
            return null;
        }

        /**
         * register new symbol specification
         *
         * @param symbol - symbol code
         * @param spec   - symbol specification
         */
        public void registerSymbol(int symbol, CoreSymbolSpecification spec)
        {
            symbolSpecs[symbol] = spec;
        }

        /**
         * Reset state
         */
        public void reset()
        {
            symbolSpecs.Clear();
        }

        public void writeMarshallable(IBytesOut bytes)
        {
            // write symbolSpecs
            SerializationUtils.marshallIntHashMap(symbolSpecs, bytes);
        }

        public int stateHash()
        {
            return 97 * HashingUtils.stateHash(symbolSpecs);
        }

    }

}
