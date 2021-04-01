//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.IO;
using System.Text;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Serialization.Surrogates;
using Alachisoft.NCache.Serialization;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Reflection;

namespace Alachisoft.NCache.IO
{
    /// <summary>
    /// This class encapsulates a <see cref="BinaryReader"/> object. It also provides an extra
    /// Read method for <see cref="System.Object"/> types. 
    /// </summary>
    public class CompactBinaryReader : CompactReader, IDisposable
    {
        private SerializationContext context;
        private BinaryReader reader;
        private Stack<long> _objectLengths = new Stack<long>();
        private long currentObjectEndOffset;

        /// <summary>
        /// Constructs a compact reader over a <see cref="Stream"/> object.
        /// </summary>
        /// <param name="input"><see cref="Stream"/> object</param>
        public CompactBinaryReader(Stream input)
            : this(input, new UTF8Encoding(true))
        {
        }
        /// <summary>
        /// Constructs a compact reader over a <see cref="Stream"/> object.
        /// </summary>
        /// <param name="input"><see cref="Stream"/> object</param>
        /// <param name="encoding"><see cref="System.Text.Encoding"/> object</param>
        public CompactBinaryReader(Stream input, Encoding encoding)
        {
            context = new SerializationContext();
            reader = new BinaryReader(input, encoding);
        }

        /// <summary> Returns the underlying <see cref="BinaryReader"/> object. </summary>
        internal BinaryReader BaseReader { get { return reader; } }

        internal void BeginCurrentObjectDeserialization(long dataLength)
        {
            currentObjectEndOffset = dataLength;
            _objectLengths.Push(dataLength);
        }

        internal void EndCurrentObjectDeserialization()
        {
            _objectLengths.Pop();
            if (_objectLengths.Count > 0)
                currentObjectEndOffset = _objectLengths.Peek();
            else
                currentObjectEndOffset = 0;
        }

        internal bool HasCurrentObjectFinished()
        {
            return reader.BaseStream.Position >= currentObjectEndOffset;
        }

        /// <summary> Returns the current <see cref="SerializationContext"/> object. </summary>
        internal SerializationContext Context { get { return context; } }

        public Stream Stream
        {
            get { return reader != null? reader.BaseStream: null; }
        }

        #region Dispose
        /// <summary>
        /// Close the underlying <see cref="BinaryWriter"/>.
        /// </summary>
        public void Dispose()
        {
            if (reader != null) reader.Close();
        }

        /// <summary>
        /// Close the underlying <see cref="BinaryWriter"/>.
        /// </summary>
        public void Dispose(bool closeStream)
        {
            if (closeStream) reader.Close();
            reader = null;
        }
        #endregion

        public override Stream BaseStream { get { return reader.BaseStream; } }

        /// <summary>
        /// Reads an object of type <see cref="object"/> from the current stream 
        /// and advances the stream position. 
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override object ReadObject()
        {
            // read type handle
            short handle = reader.ReadInt16();         
            // Find an appropriate surrogate by handle
            ISerializationSurrogate surrogate =
                TypeSurrogateSelector.GetSurrogateForTypeHandle(handle, context.CacheContext);


            if (surrogate == null)
            {
                surrogate = TypeSurrogateSelector.GetSurrogateForSubTypeHandle(handle, reader.ReadInt16(), context.CacheContext);
            }

            //If surrogate not found defaultSurrogate is returned
            //if (surrogate == null) throw new CompactSerializationException("Type handle " + handle + " is not registered with Compact Serialization Framework");

            object obj = null;
            try
            {
                obj = surrogate.Read(this);
            }
            catch (CompactSerializationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new CompactSerializationException(e.Message,e);
            }

            return obj;
        }

        public override T ReadObjectAs<T>()
        {
            // Find an appropriate surrogate by type
            ISerializationSurrogate surrogate =
                TypeSurrogateSelector.GetSurrogateForType(typeof(T), context.CacheContext);

            return (T)surrogate.Read(this);
        }

        /// <summary>
        /// Skips an object of type <see cref="object"/> from the current stream 
        /// and advances the stream position. 
        /// </summary>
        public override void SkipObject()
        {
            // read type handle
            short handle = reader.ReadInt16();
            // Find an appropriate surrogate by handle
            ISerializationSurrogate surrogate =
                TypeSurrogateSelector.GetSurrogateForTypeHandle(handle, context.CacheContext);

            if (surrogate == null)
            {
                surrogate = TypeSurrogateSelector.GetSurrogateForSubTypeHandle(handle, reader.ReadInt16(), context.CacheContext);
            }

            //If surrogate not found, returns defaultSurrogate
            //if (surrogate == null) throw new CompactSerializationException("Type handle " + handle + " is not registered with Compact Serialization Framework");

            try
            {
                surrogate.Skip(this);
            }
            catch (CompactSerializationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new CompactSerializationException(e.Message);
            }
        }

        public override void SkipObjectAs<T>()
        {
            // Find an appropriate surrogate by type
            ISerializationSurrogate surrogate =
                TypeSurrogateSelector.GetSurrogateForType(typeof(T), context.CacheContext);

            surrogate.Skip(this);
        }

        public object IfSkip(object readObjectValue, object defaultValue)
        {
            if (readObjectValue is SkipSerializationSurrogate)
                return defaultValue;
            else
                return readObjectValue;
        }

        public string CacheContext
        {
            get { return context.CacheContext; }
        }

        #region /      CompactBinaryReader.ReadXXX      /

        /// <summary>
        /// Reads an object of type <see cref="bool"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override bool ReadBoolean() { return reader.ReadBoolean(); }
        /// <summary>
        /// Reads an object of type <see cref="byte"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override byte ReadByte() { return reader.ReadByte(); }
        /// <summary>
        /// Reads an object of type <see cref="byte[]"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="count">number of bytes read</param>
        /// <returns>object read from the stream</returns>
        public override byte[] ReadBytes(int count) { return reader.ReadBytes(count); }
        /// <summary>
        /// Reads an object of type <see cref="char"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override char ReadChar() { return reader.ReadChar(); }
        /// <summary>
        /// Reads an object of type <see cref="char[]"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override char[] ReadChars(int count) { return reader.ReadChars(count); }
        /// <summary>
        /// Reads an object of type <see cref="decimal"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override decimal ReadDecimal() { return reader.ReadDecimal(); }
        /// <summary>
        /// Reads an object of type <see cref="float"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override float ReadSingle() { return reader.ReadSingle(); }
        /// <summary>
        /// Reads an object of type <see cref="double"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override double ReadDouble() { return reader.ReadDouble(); }
        /// <summary>
        /// Reads an object of type <see cref="short"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override short ReadInt16() { return reader.ReadInt16(); }
        /// <summary>
        /// Reads an object of type <see cref="int"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override int ReadInt32() { return reader.ReadInt32(); }
        /// <summary>
        /// Reads an object of type <see cref="long"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override long ReadInt64() { return reader.ReadInt64(); }
        /// <summary>
        /// Reads an object of type <see cref="string"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override string ReadString() { return reader.ReadString(); }
        /// <summary>
        /// Reads an object of type <see cref="DateTime"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override DateTime ReadDateTime() { return new DateTime(reader.ReadInt64()); }
        /// <summary>
        /// Reads an object of type <see cref="Guid"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public override Guid ReadGuid() { return new Guid(reader.ReadBytes(16)); }
        /// <summary>
        /// Reads the specifies number of bytes into <paramref name="buffer"/>.
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="buffer">buffer to read into</param>
        /// <param name="index">starting position in the buffer</param>
        /// <param name="count">number of bytes to write</param>
        /// <returns>number of buffer read</returns>
        public override int Read(byte[] buffer, int index, int count)
        {
            if (HasCurrentObjectFinished())
                return 0;

            return reader.Read(buffer, index, count);
        }
        /// <summary>
        /// Reads the specifies number of bytes into <paramref name="buffer"/>.
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="buffer">buffer to read into</param>
        /// <param name="index">starting position in the buffer</param>
        /// <param name="count">number of bytes to write</param>
        /// <returns>number of chars read</returns>
        public override int Read(char[] buffer, int index, int count)
        {
            if (HasCurrentObjectFinished())
                return 0;

            return reader.Read(buffer, index, count);
        }
        /// <summary>
        /// Reads an object of type <see cref="sbyte"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        [CLSCompliant(false)]
        public override sbyte ReadSByte() { return reader.ReadSByte(); }
        /// <summary>
        /// Reads an object of type <see cref="ushort"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        [CLSCompliant(false)]
        public override ushort ReadUInt16() { return reader.ReadUInt16(); }
        /// <summary>
        /// Reads an object of type <see cref="uint"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        [CLSCompliant(false)]
        public override uint ReadUInt32() { return reader.ReadUInt32(); }
        /// <summary>
        /// Reads an object of type <see cref="ulong"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        [CLSCompliant(false)]
        public override ulong ReadUInt64() { return reader.ReadUInt64(); }

        public override object ReadObject(object defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;
   
            return ReadObject();
        }

        public override bool ReadBoolean(bool defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;
   
            return ReadBoolean();
        }

        public override byte ReadByte(byte defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadByte();
        }

        public override byte[] ReadBytes(int count, byte[] defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadBytes(count);
        }

        public override char ReadChar(char defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadChar();
        }

        public override char[] ReadChars(int count, char[] defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadChars(count);
        }

        public override decimal ReadDecimal(decimal defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadDecimal();
        }

        public override float ReadSingle(float defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadSingle();
        }

        public override double ReadDouble(double defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadDouble();
        }

        public override short ReadInt16(short defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadInt16();
        }

        public override int ReadInt32(int defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadInt32();
        }

        public override long ReadInt64(long defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadInt64();
        }

        public override string ReadString(string defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadString();
        }

        public override DateTime ReadDateTime(DateTime defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadDateTime();
        }

        public override Guid ReadGuid(Guid defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadGuid();
        }

        public override sbyte ReadSByte(sbyte defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadSByte();
        }

        public override ushort ReadUInt16(ushort defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadUInt16();
        }

        public override uint ReadUInt32(uint defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadUInt32();
        }

        public override ulong ReadUInt64(ulong defaultValue)
        {
            if (HasCurrentObjectFinished())
                return defaultValue;

            return ReadUInt64();
        }

        #endregion


        #region /      CompactBinaryReader.SkipXXX      /

        /// <summary>
        /// Skips an object of type <see cref="bool"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipBoolean()
        {
            reader.BaseStream.Position = reader.BaseStream.Position++;
        }

        /// <summary>
        /// Skips an object of type <see cref="byte"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipByte()
        {
            reader.BaseStream.Position = reader.BaseStream.Position++;
        }

        /// <summary>
        /// Skips an object of type <see cref="byte[]"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        /// <param name="count">number of bytes read</param>
        public override void SkipBytes(int count)
        {
            reader.BaseStream.Position = reader.BaseStream.Position + count;
        }

        /// <summary>
        /// Skips an object of type <see cref="char"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipChar()
        {
            reader.BaseStream.Position = reader.BaseStream.Position++;
        }

        /// <summary>
        /// Skips an object of type <see cref="char[]"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipChars(int count)
        {
            reader.BaseStream.Position = reader.BaseStream.Position + count;
        }

        /// <summary>
        /// Skips an object of type <see cref="decimal"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipDecimal()
        {
            reader.BaseStream.Position = reader.BaseStream.Position + 16;
        }

        /// <summary>
        /// Skips an object of type <see cref="float"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipSingle()
        {
            reader.BaseStream.Position = reader.BaseStream.Position + 4;
        }

        /// <summary>
        /// Skips an object of type <see cref="double"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipDouble()
        {
            reader.BaseStream.Position = reader.BaseStream.Position + 8;
        }

        /// <summary>
        /// Skips an object of type <see cref="short"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipInt16()
        {
            reader.BaseStream.Position = reader.BaseStream.Position + 2;
        }

        /// <summary>
        /// Skips an object of type <see cref="int"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipInt32()
        {
            reader.BaseStream.Position = reader.BaseStream.Position + 4;
        }

        /// <summary>
        /// Skips an object of type <see cref="long"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipInt64()
        {
            reader.BaseStream.Position = reader.BaseStream.Position + 8;
        }

        /// <summary>
        /// Skips an object of type <see cref="string"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipString()
        {
            //Reads String but does not assign value in effect behaves as a string
            //Reads a string from the current stream. The string is prefixed with the length, encoded as an integer seven bits at a time.
            reader.ReadString();
        }

        /// <summary>
        /// Skips an object of type <see cref="DateTime"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipDateTime()
        {
            this.SkipInt64();
        }

        /// <summary>
        /// Skips an object of type <see cref="Guid"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipGuid()
        {
            reader.BaseStream.Position = reader.BaseStream.Position + 16;
        }

        /// <summary>
        /// Skips an object of type <see cref="sbyte"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipSByte()
        {
            reader.BaseStream.Position++;
        }

        /// <summary>
        /// Skips an object of type <see cref="ushort"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipUInt16()
        {
            reader.BaseStream.Position = reader.BaseStream.Position + 2;
        }

        /// <summary>
        /// Skips an object of type <see cref="uint"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipUInt32()
        {
            reader.BaseStream.Position = reader.BaseStream.Position + 4;
        }

        /// <summary>
        /// Skips an object of type <see cref="ulong"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public override void SkipUInt64()
        {
            reader.BaseStream.Position = reader.BaseStream.Position + 8;
        }

      
        #endregion

    }
}
