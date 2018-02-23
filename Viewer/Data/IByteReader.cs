using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public interface IByteReader : IDisposable
    {
        /// <summary>
        /// Current position in the input stream
        /// </summary>
        long Position { get; }

        /// <summary>
        /// True iff the end is reached
        /// </summary>
        bool IsEnd { get; }

        /// <summary>
        /// Read byte from the input and advance current position
        /// </summary>
        /// <exception cref="EndOfStreamException">The end of the stream is reached</exception>
        /// <returns>Next byte in the input</returns>
        byte ReadByte();

        /// <summary>
        /// Read int16 (2s complement, Little Endian) from the input and advance current position.
        /// </summary>
        /// <exception cref="EndOfStreamException">The end of the stream is reached</exception>
        /// <returns>Next int16 in the input</returns>
        short ReadInt16();

        /// <summary>
        /// Read int32 (2s complement, Little Endian) from the input and advance current position.
        /// </summary>
        /// <exception cref="EndOfStreamException">The end of the stream is reached</exception>
        /// <returns>Next int32 in the input</returns>
        int ReadInt32();

        /// <summary>
        /// Read double (IEEE 754 binary64) from the input and advance current position.
        /// </summary>
        /// <exception cref="EndOfStreamException">The end of the stream is reached</exception>
        /// <returns>Next double in the input</returns>
        double ReadDouble();
    }

    /// <summary>
    /// ByteReader base class which defines methods for parsing the specialized types
    /// Actual implementation just has to provide the ReadByte, Dispose methods and 
    /// Position, Length properties.
    /// </summary>
    public abstract class ByteReader : IByteReader
    {
        public abstract long Position { get; }

        public abstract bool IsEnd { get; }

        public abstract byte ReadByte();

        public abstract void Dispose();

        public short ReadInt16()
        {
            var low = ReadByte();
            var high = ReadByte();
            return (short)((high << 8) | low);
        }

        public int ReadInt32()
        {
            int value = 0;
            int shift = 0;
            for (int i = 0; i < 4; ++i)
            {
                value |= ReadByte() << shift;
                shift += 8;
            }

            return value;
        }
        
        public double ReadDouble()
        {
            var buffer = new byte[8];
            for (int i = 0; i < 8; ++i)
            {
                buffer[i] = ReadByte();
            }

            return BitConverter.ToDouble(buffer, 0);
        }
    }
}
