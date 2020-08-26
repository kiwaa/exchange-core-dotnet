using System;

namespace Exchange.Core.Common.Config
{
    public class SerializationConfiguration
    {
        // no serialization
        public static readonly SerializationConfiguration DEFAULT = builder()
            .enableJournaling(false)
            .serializationProcessorFactory(cfg => DummySerializationProcessor.INSTANCE)
            .build();

        private static SerializationConfigurationBuilder builder()
        {
            return new SerializationConfigurationBuilder();
        }

        private class SerializationConfigurationBuilder
        {
            internal SerializationConfigurationBuilder enableJournaling(bool v)
            {
                throw new NotImplementedException();
            }

            internal SerializationConfigurationBuilder serializationProcessorFactory(Func<object, DummySerializationProcessor> iNSTANCE)
            {
                throw new NotImplementedException();
            }

            internal SerializationConfiguration build()
            {
                throw new NotImplementedException();
            }
        }
    }
}