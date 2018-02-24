using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public interface IByteWriter
    {
        /// <summary>
        /// Write byte value
        /// </summary>
        /// <param name="value">Byte to write</param>
        void WriteByte(byte value);
        
        /// <summary>
        /// Write int16 as little-endian
        /// </summary>
        /// <param name="value">Value to write</param>
        void WriteInt16(short value);
        
        /// <summary>
        /// Write int32 as little-endian
        /// </summary>
        /// <param name="value">Value to write</param>
        void WriteInt32(int value);
        
        /// <summary>
        /// Write double (IEEE 754 binary64)
        /// </summary>
        /// <param name="value">Value to write</param>
        void WriteDouble(double value);

        /// <summary>
        /// Write all bytes in an array
        /// </summary>
        /// <param name="buffer">Array of bytes to write</param>
        void WriteBytes(byte[] buffer);
    }

    /// <summary>
    /// Basic implementation which requires derived types to define only 1 method.
    /// </summary>
    public abstract class ByteWriter : IByteWriter
    {
        public abstract void WriteByte(byte value);

        public void WriteInt16(short value)
        {
            var high = (byte) (value >> 8);
            var low = (byte) (value & 0xFF);
            WriteByte(low);
            WriteByte(high);
        }

        public void WriteInt32(int value)
        {
            for (int i = 0; i < 4; ++i)
            {
                WriteByte((byte) (value & 0xFF));
                value >>= 8;
            }
        }

        public void WriteDouble(double value)
        {
            var bytes = BitConverter.GetBytes(value);
            foreach (var val in bytes)
            {
                WriteByte(val);
            }
        }

        public void WriteBytes(byte[] buffer)
        {
            foreach (var value in buffer)
            {
                WriteByte(value);
            }
        }
    }
}
