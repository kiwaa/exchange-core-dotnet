using Exchange.Core.Common;

namespace Exchange.Core.Tests.Examples
{
    internal class BatchAddSymbolsCommand
    {
        private CoreSymbolSpecification symbolSpecXbtLtc;

        public BatchAddSymbolsCommand(CoreSymbolSpecification symbolSpecXbtLtc)
        {
            this.symbolSpecXbtLtc = symbolSpecXbtLtc;
        }
    }
}