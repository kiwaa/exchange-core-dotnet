using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenHFT.Chronicle.WireMock
{
    public class NativeBytes : IBytesIn, IBytesOut
    {
        private byte[] _buffer;
        int _position = 0;

        public NativeBytes(int size)
        {
            _buffer = new byte[size];
        }
        public NativeBytes(byte[] buf)
        {
            _buffer = buf;
        }

        public int readRemaining()
        {
            return _position;
        }

        public IBytesOut writeLong(long p)
        {
            Write(p, sizeof(long));
            return this;
        }

        public IBytesOut writeInt(int p)
        {
            Write(p, sizeof(int));
            return this;
        }

        public IBytesOut writeByte(sbyte direction)
        {
            _buffer[_position] = (byte)direction;
            _position += 1;
            return this;
        }

        public IBytesOut writeBool(bool v)
        {
            Write(v, sizeof(bool));
            return this;

        }
        public Span<byte> read()
        {
            return _buffer.AsSpan(0, _position);
        }

        public int readInt()
        {
            var span = _buffer.AsSpan<byte>(_position);
            var tmp = MemoryMarshal.Read<int>(span);
            _position += sizeof(int);
            return tmp;
        }

        public long readLong()
        {
            var span = _buffer.AsSpan<byte>(_position);
            var tmp = MemoryMarshal.Read<long>(span);
            _position += sizeof(long);
            return tmp;
        }

        public sbyte readByte()
        {
            var tmp = (sbyte)_buffer[_position];
            _position ++;
            return tmp;
        }
        public bool readBool()
        {
            return Read<bool>(sizeof(bool));
        }

        private void Write<T>(T value, int size) where T: struct
        {
            var span = _buffer.AsSpan<byte>(_position);
            if (span.Length < size)
            {
                Resize(_buffer.Length * 2);
                span = _buffer.AsSpan<byte>(_position);
            }
            MemoryMarshal.Write(span, ref value);
            _position += size;
        }

        private T Read<T>(int size) where T : struct
        {
            var span = _buffer.AsSpan<byte>(_position);
            var tmp = MemoryMarshal.Read<T>(span);
            _position += size;
            return tmp;

        }

        private void Resize(int v)
        {
            var tmp = new byte[v];
            Array.Copy(_buffer, tmp, _buffer.Length);
            _buffer = tmp;
        }
    }
}
