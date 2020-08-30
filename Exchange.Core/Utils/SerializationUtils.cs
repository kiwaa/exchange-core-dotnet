using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Utils
{
    public sealed class SerializationUtils
    {
        public static long[] bytesToLongArray(NativeBytes<byte> bytes, int padding)
        {
            //ByteBuffer byteBuffer = ByteBuffer.allocate();
            using (var byteBuffer = new MemoryStream((int)bytes.readRemaining()))
            {
                bytes.read(byteBuffer);
                byte[] array = byteBuffer.ToArray();
                //        log.debug("array:{}", array);
                long[] longs = toLongsArray(array, padding);
                //        log.debug("longs:{}", longs);
                return longs;
            }
        }

        //public static long[] bytesToLongArrayLz4(LZ4Compressor lz4Compressor, NativeBytes<Void> bytes, int padding)
        //{
        //    int originalSize = (int)bytes.readRemaining();
        //    //        log.debug("COMPRESS originalSize={}", originalSize);

        //    ByteBuffer byteBuffer = ByteBuffer.allocate(originalSize);

        //    bytes.read(byteBuffer);

        //    byteBuffer.flip();

        //    ByteBuffer byteBufferCompressed = ByteBuffer.allocate(4 + lz4Compressor.maxCompressedLength(originalSize));
        //    byteBufferCompressed.putInt(originalSize);// override with compressed length
        //    lz4Compressor.compress(byteBuffer, byteBufferCompressed);

        //    byteBufferCompressed.flip();

        //    int compressedBytesLen = byteBufferCompressed.remaining();

        //    return toLongsArray(
        //            byteBufferCompressed.array(),
        //            byteBufferCompressed.arrayOffset(),
        //            compressedBytesLen,
        //            padding);
        //}

        public static long[] toLongsArray(byte[] bytes, int padding)
        {
                int longLength = requiredLongArraySize(bytes.Length, padding);
            long[] longArray = new long[longLength];
            //log.debug("byte[{}]={}", bytes.length, bytes);
            using (var allocate = new MemoryStream(longLength * 8 * 2))
            {
                //ByteBuffer allocate = ByteBuffer.allocate(longLength * 8 * 2);
                LongBuffer longBuffer = allocate.asLongBuffer();
                allocate.Write(bytes);
                longBuffer.get(longArray);
                return longArray;
            }
        }

        //public static long[] toLongsArray(byte[] bytes, int offset, int length, int padding)
        //{

        //    int longLength = requiredLongArraySize(length, padding);
        //    long[] longArray = new long[longLength];
        //    //log.debug("byte[{}]={}", bytes.length, bytes);
        //    ByteBuffer allocate = ByteBuffer.allocate(longLength * 8 * 2);
        //    LongBuffer longBuffer = allocate.asLongBuffer();
        //    allocate.put(bytes, offset, length);
        //    longBuffer.get(longArray);
        //    return longArray;
        //}


        public static int requiredLongArraySize(int bytesLength, int padding)
        {
            int len = requiredLongArraySize(bytesLength);
            if (padding == 1)
            {
                return len;
            }
            else
            {
                int rem = len % padding;
                return rem == 0 ? len : (len + padding - rem);
            }
        }


        //public static Wire longsToWire(long[] dataArray)
        //{

        //    int sizeInBytes = dataArray.Length * 8;
        //    ByteBuffer byteBuffer = ByteBuffer.allocate(sizeInBytes);
        //    byteBuffer.asLongBuffer().put(dataArray);

        //    byte[] bytesArray = new byte[sizeInBytes];
        //    byteBuffer.get(bytesArray);

        //    //log.debug(" section {} -> {}", section, bytes);

        //    Bytes<ByteBuffer> bytes = Bytes.elasticHeapByteBuffer(sizeInBytes);
        //    bytes.ensureCapacity(sizeInBytes);

        //    bytes.write(bytesArray);

        //    return WireType.RAW.apply(bytes);
        //}

        //public static Wire longsLz4ToWire(long[] dataArray, int longsTransfered)
        //{

        //    //        log.debug("long dataArray.len={} longsTransfered={}", dataArray.length, longsTransfered);

        //    ByteBuffer byteBuffer = ByteBuffer.allocate(longsTransfered * 8);
        //    byteBuffer.asLongBuffer().put(dataArray, 0, longsTransfered);

        //    int originalSizeBytes = byteBuffer.getInt();

        //    ByteBuffer uncompressedByteBuffer = ByteBuffer.allocate(originalSizeBytes);

        //    LZ4FastDecompressor lz4FastDecompressor = LZ4Factory.fastestInstance().fastDecompressor();

        //    lz4FastDecompressor.decompress(byteBuffer, byteBuffer.position(), uncompressedByteBuffer, uncompressedByteBuffer.position(), originalSizeBytes);

        //    Bytes<ByteBuffer> bytes = Bytes.wrapForRead(uncompressedByteBuffer);

        //    return WireType.RAW.apply(bytes);
        //}


        public static int requiredLongArraySize(int bytesLength)
        {
            return ((bytesLength - 1) >> 3) + 1;
        }

        //public static void marshallBitSet(BitSet bitSet, BytesOut bytes)
        //{
        //    marshallLongArray(bitSet.toLongArray(), bytes);
        //}

        //public static BitSet readBitSet(final BytesIn bytes)
        //{
        //    // TODO use LongBuffer
        //    return BitSet.valueOf(readLongArray(bytes));
        //}


        //public static void marshallLongArray(long[] longs, BytesOut bytes)
        //{
        //    bytes.writeInt(longs.length);
        //    for (long word : longs)
        //    {
        //        bytes.writeLong(word);
        //    }
        //}


        public static long[] readLongArray(IBytesIn bytes)
        {
            int length = bytes.readInt();
            long[] array = new long[length];
            // TODO read byte[], then convert into long[]
            for (int i = 0; i < length; i++)
            {
                array[i] = bytes.readLong();
            }
            return array;
        }

        //public static void marshallLongIntHashMap(final MutableLongIntMap hashMap, final BytesOut bytes)
        //{

        //    bytes.writeInt(hashMap.size());
        //    hashMap.forEachKeyValue((k, v)-> {
        //        bytes.writeLong(k);
        //        bytes.writeInt(v);
        //    });
        //}

        //public static LongIntHashMap readLongIntHashMap(final BytesIn bytes)
        //{
        //    int length = bytes.readInt();
        //    LongIntHashMap hashMap = new LongIntHashMap(length);
        //    // TODO shuffle (? performance can be reduced if populating linearly)
        //    for (int i = 0; i < length; i++)
        //    {
        //        long k = bytes.readLong();
        //        int v = bytes.readInt();
        //        hashMap.put(k, v);
        //    }
        //    return hashMap;
        //}

        //public static void marshallIntLongHashMap(final MutableIntLongMap hashMap, final BytesOut bytes)
        //{

        //    bytes.writeInt(hashMap.size());

        //    hashMap.forEachKeyValue((k, v)-> {
        //        bytes.writeInt(k);
        //        bytes.writeLong(v);
        //    });
        //}

        public static Dictionary<int,long> readIntLongHashMap(IBytesIn bytes)
        {
            int length = bytes.readInt();
            Dictionary<int, long> hashMap = new Dictionary<int, long>(length);
            // TODO shuffle (? performance can be reduced if populating linearly)
            for (int i = 0; i < length; i++)
            {
                int k = bytes.readInt();
                long v = bytes.readLong();
                hashMap[k] = v;
            }
            return hashMap;
        }


        //public static void marshallLongHashSet(final LongHashSet set, final BytesOut bytes)
        //{
        //    bytes.writeInt(set.size());
        //    set.forEach(bytes::writeLong);
        //}

        //public static LongHashSet readLongHashSet(final BytesIn bytes)
        //{
        //    int length = bytes.readInt();
        //    final LongHashSet set = new LongHashSet(length);
        //    // TODO shuffle (? performance can be reduced if populating linearly)
        //    for (int i = 0; i < length; i++)
        //    {
        //        set.add(bytes.readLong());
        //    }
        //    return set;
        //}


        //public static <T extends WriteBytesMarshallable> void marshallLongHashMap(final LongObjectHashMap<T> hashMap, final BytesOut bytes)
        //{

        //    bytes.writeInt(hashMap.size());

        //    hashMap.forEachKeyValue((k, v)-> {
        //        bytes.writeLong(k);
        //        v.writeMarshallable(bytes);
        //    });

        //}

        //public static <T> void marshallLongHashMap(final LongObjectHashMap<T> hashMap, final BiConsumer<T, BytesOut> valuesMarshaller, final BytesOut bytes)
        //{

        //    bytes.writeInt(hashMap.size());

        //    hashMap.forEachKeyValue((k, v)-> {
        //        bytes.writeLong(k);
        //        valuesMarshaller.accept(v, bytes);
        //    });

        //}

        public static Dictionary<long, T> readLongHashMap<T>(IBytesIn bytes, Func<IBytesIn, T> creator)
        {
            int length = bytes.readInt();
            Dictionary<long, T> hashMap = new Dictionary<long, T>(length);
            for (int i = 0; i < length; i++)
            {
                hashMap[bytes.readLong()] = creator(bytes);
            }
            return hashMap;
        }

        //public static <T extends WriteBytesMarshallable> void marshallIntHashMap(final IntObjectHashMap<T> hashMap, final BytesOut bytes)
        //{
        //    bytes.writeInt(hashMap.size());
        //    hashMap.forEachKeyValue((k, v)-> {
        //        bytes.writeInt(k);
        //        v.writeMarshallable(bytes);
        //    });
        //}

        //public static <T> void marshallIntHashMap(final IntObjectHashMap<T> hashMap, final BytesOut bytes, final Consumer<T> elementMarshaller)
        //{
        //    bytes.writeInt(hashMap.size());
        //    hashMap.forEachKeyValue((k, v)-> {
        //        bytes.writeInt(k);
        //        elementMarshaller.accept(v);
        //    });
        //}

        public static Dictionary<int, T> readIntHashMap<T>(IBytesIn bytes, Func<IBytesIn, T> creator)
        {
            int length = bytes.readInt();
            Dictionary<int, T> hashMap = new Dictionary<int, T>(length);
            for (int i = 0; i < length; i++)
            {
                hashMap[bytes.readInt()] = creator(bytes);
            }
            return hashMap;
        }


        //public static <T extends WriteBytesMarshallable> void marshallLongMap(final Map<Long, T> map, final BytesOut bytes)
        //{
        //    bytes.writeInt(map.size());

        //    map.forEach((k, v)-> {
        //        bytes.writeLong(k);
        //        v.writeMarshallable(bytes);
        //    });
        //}

        public static M readLongMap<T,M>(IBytesIn bytes, Func<M> mapSupplier, Func<IBytesIn, T> creator) where M : IDictionary<long, T>
        {
            int length = bytes.readInt();
            M map = mapSupplier();
            for (int i = 0; i < length; i++)
            {
                map[bytes.readLong()] = creator(bytes);
            }
            return map;
        }

        //public static <K, V> void marshallGenericMap(final Map<K, V> map,
        //                                             final BytesOut bytes,
        //                                             final BiConsumer<BytesOut, K> keyMarshaller,
        //                                             final BiConsumer<BytesOut, V> valMarshaller)
        //{
        //    bytes.writeInt(map.size());

        //    map.forEach((k, v)-> {
        //        keyMarshaller.accept(bytes, k);
        //        valMarshaller.accept(bytes, v);
        //    });
        //}

        //public static <K, V, M extends Map<K, V>> M readGenericMap(final BytesIn bytes,
        //                                                           final Supplier<M> mapSupplier,
        //                                                           final Function<BytesIn, K> keyCreator,
        //                                                           final Function<BytesIn, V> valCreator)
        //{
        //    int length = bytes.readInt();
        //    final M map = mapSupplier.get();
        //    for (int i = 0; i < length; i++)
        //    {
        //        map.put(keyCreator.apply(bytes), valCreator.apply(bytes));
        //    }
        //    return map;
        //}

        //public static <T extends WriteBytesMarshallable> void marshallList(final List<T> list, final BytesOut bytes)
        //{
        //    bytes.writeInt(list.size());
        //    list.forEach(v->v.writeMarshallable(bytes));
        //}

        //public static <T> List<T> readList(final BytesIn bytes, final Function<BytesIn, T> creator)
        //{
        //    final int length = bytes.readInt();
        //    final List<T> list = new ArrayList<>(length);
        //    for (int i = 0; i < length; i++)
        //    {
        //        list.add(creator.apply(bytes));
        //    }
        //    return list;
        //}

        //public static <T> void marshallNullable(final T object, final BytesOut bytes, final BiConsumer<T, BytesOut> marshaller)
        //{
        //    bytes.writeBoolean(object != null);
        //    if (object != null)
        //    {
        //        marshaller.accept(object, bytes);
        //    }
        //}

        //public static <T> T preferNotNull(final T a, final T b)
        //{
        //    return a == null ? b : a;
        //}

        //public static <T> T readNullable(final BytesIn bytesIn, final Function<BytesIn, T> creator)
        //{
        //    return bytesIn.readBoolean() ? creator.apply(bytesIn) : null;
        //}

        //public static <V> LongObjectHashMap<V> mergeOverride(final LongObjectHashMap<V> a, final LongObjectHashMap<V> b)
        //{
        //    final LongObjectHashMap<V> res = a == null ? new LongObjectHashMap<>() : new LongObjectHashMap<>(a);
        //    if (b != null)
        //    {
        //        res.putAll(b);
        //    }
        //    return res;
        //}

        //public static <V> IntObjectHashMap<V> mergeOverride(final IntObjectHashMap<V> a, final IntObjectHashMap<V> b)
        //{
        //    final IntObjectHashMap<V> res = a == null ? new IntObjectHashMap<>() : new IntObjectHashMap<>(a);
        //    if (b != null)
        //    {
        //        res.putAll(b);
        //    }
        //    return res;
        //}

        //public static IntLongHashMap mergeSum(final IntLongHashMap... maps)
        //{
        //    IntLongHashMap res = null;
        //    for (IntLongHashMap map : maps)
        //    {
        //        if (map != null)
        //        {
        //            if (res == null)
        //            {
        //                res = new IntLongHashMap(map);
        //            }
        //            else
        //            {
        //                map.forEachKeyValue(res::addToValue);
        //            }
        //        }
        //    }
        //    return res != null ? res : new IntLongHashMap();
        //}

    }
}
