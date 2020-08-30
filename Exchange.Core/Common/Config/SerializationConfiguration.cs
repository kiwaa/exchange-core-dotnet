using System;

namespace Exchange.Core.Common.Config
{
    public sealed partial class SerializationConfiguration
    {
        // no serialization
        public static readonly SerializationConfiguration DEFAULT = Builder()
            .enableJournaling(false)
            .serializationProcessorFactory(cfg => DummySerializationProcessor.INSTANCE)
            .build();

        // no journaling, only snapshots
        public static SerializationConfiguration DISK_SNAPSHOT_ONLY = Builder()
            .enableJournaling(false)
            .serializationProcessorFactory(exchangeCfg => new DiskSerializationProcessor(exchangeCfg, DiskSerializationProcessorConfiguration.createDefaultConfig()))
            .build();

        // snapshots and journaling
        public static SerializationConfiguration DISK_JOURNALING = Builder()
            .enableJournaling(true)
            .serializationProcessorFactory(exchangeCfg => new DiskSerializationProcessor(exchangeCfg, DiskSerializationProcessorConfiguration.createDefaultConfig()))
            .build();
    }
}