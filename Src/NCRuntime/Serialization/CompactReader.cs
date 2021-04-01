//  Copyright (c) 2018 Alachisoft
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
using System.Text;
using System.Reflection;
using System.IO;

/// <summary>
/// The namespace provides the CompactReader and CompactWriter that are required 
/// while implementing the Serialize and Deserialize methods of ICompactSerializable interface.
/// </summary>

namespace Alachisoft.NCache.Runtime.Serialization.IO
{
    /// <summary>
    /// CompactReader is the  base class for CompactBinaryReader.
    /// </summary>

    //[CLSCompliant(false)]
    public abstract class CompactReader
    {
        /// <summary>
        /// Reads an object of type <see cref="object"/> from the current stream 
        /// and advances the stream position. 
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract object ReadObject();

        /// <summary>
        /// Reads an object of type <see cref="object"/> from the current stream 
        /// and advances the stream position. 
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract object ReadObject(object defaultValue);

        /// <summary>
        /// Reads an object of specified type from the current stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public abstract T ReadObjectAs<T>();

        /// <summary>
        /// Memory stream on which the bytes are read from
        /// </summary>
        public abstract Stream BaseStream { get; }
        /// <summary>
        /// Skips an object of type <see cref="object"/> from the current stream 
        /// and advances the stream position. 
        /// </summary>
        public abstract void SkipObject();

        /// <summary>
        /// Skips an object of template type from the current stream and advances the stream position.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public abstract void SkipObjectAs<T>();

        #region /      CompactBinaryReader.ReadXXX      /

        /// <summary>
        /// Reads an object of type <see cref="bool"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract bool ReadBoolean();

        /// <summary>
        /// Reads an object of type <see cref="bool"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>bool read from the stream</returns>
        public abstract bool ReadBoolean(bool defaultValue);

        /// <summary>
        /// Reads an object of type <see cref="byte"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract byte ReadByte();

        /// <summary>
        /// Reads an object of type <see cref="byte"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract byte ReadByte(byte defaultValue);

        /// <summary>
        /// Reads an object of type <see cref="byte[]"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="count">number of bytes read</param>
        /// <returns>object read from the stream</returns>
        public abstract byte[] ReadBytes(int count);

        /// <summary>
        /// Reads an object of type <see cref="byte[]"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="count">number of bytes read</param>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract byte[] ReadBytes(int count,byte[] defaultValue);

        /// <summary>
        /// Reads an object of type <see cref="char"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract char ReadChar();

        /// <summary>
        /// Reads an object of type <see cref="char"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract char ReadChar(char defaultValue);

        /// <summary>
        /// Reads an object of type <see cref="char[]"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract char[] ReadChars(int count);

        /// <summary>
        /// Reads an object of type <see cref="char[]"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract char[] ReadChars(int count,char[] defaultValue);

        /// <summary>
        /// Reads an object of type <see cref="decimal"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract decimal ReadDecimal();

        /// <summary>
        /// Reads an object of type <see cref="decimal"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract decimal ReadDecimal(decimal defaultValue);

        /// <summary>
        /// Reads an object of type <see cref="float"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract float ReadSingle();

        /// <summary>
        /// Reads an object of type <see cref="float"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract float ReadSingle(float defaultValue);

        /// <summary>
        /// Reads an object of type <see cref="double"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract double ReadDouble();

        /// <summary>
        /// Reads an object of type <see cref="double"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract double ReadDouble(double defaultValue);

        /// <summary>
        /// Reads an object of type <see cref="short"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract short ReadInt16();

        /// <summary>
        /// Reads an object of type <see cref="short"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract short ReadInt16(short defaultValue);

        /// <summary>
        /// Reads an object of type <see cref="int"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract int ReadInt32();

        /// <summary>
        /// Reads an object of type <see cref="int"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract int ReadInt32(int defaultValue);

        /// <summary>
        /// Reads an object of type <see cref="long"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract long ReadInt64();

        /// <summary>
        /// Reads an object of type <see cref="long"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract long ReadInt64(long deafultValue);

        /// <summary>
        /// Reads an object of type <see cref="string"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract string ReadString();

        /// <summary>
        /// Reads an object of type <see cref="string"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract string ReadString(string defultValue);

        /// <summary>
        /// Reads an object of type <see cref="DateTime"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract DateTime ReadDateTime();

        /// <summary>
        /// Reads an object of type <see cref="DateTime"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract DateTime ReadDateTime(DateTime defaultValue);

        /// <summary>
        /// Reads an object of type <see cref="Guid"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        public abstract Guid ReadGuid();

        /// <summary>
        /// Reads an object of type <see cref="Guid"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        public abstract Guid ReadGuid(Guid defaultValue);

        /// <summary>
        /// Reads the specifies number of bytes into <paramref name="buffer"/>.
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="buffer">buffer to read into</param>
        /// <param name="index">starting position in the buffer</param>
        /// <param name="count">number of bytes to write</param>
        /// <returns>number of buffer read</returns>
        public abstract int Read(byte[] buffer, int index, int count);

        /// <summary>
        /// Reads the specifies number of bytes into <paramref name="buffer"/>.
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="buffer">buffer to read into</param>
        /// <param name="index">starting position in the buffer</param>
        /// <param name="count">number of bytes to write</param>
        /// <returns>number of chars read</returns>
        public abstract int Read(char[] buffer, int index, int count);

        /// <summary>
        /// Reads an object of type <see cref="sbyte"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        [CLSCompliant(false)]
        public virtual sbyte ReadSByte() { return 0; }


        /// <summary>
        /// Reads an object of type <see cref="sbyte"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        [CLSCompliant(false)]
        public virtual sbyte ReadSByte(sbyte defaultValue) { return 0; }

        /// <summary>
        /// Reads an object of type <see cref="ushort"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        [CLSCompliant(false)]
        public virtual ushort ReadUInt16() { return 0; }

        /// <summary>
        /// Reads an object of type <see cref="ushort"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        [CLSCompliant(false)]
        public virtual ushort ReadUInt16(ushort defaultValue) { return 0; }

        /// <summary>
        /// Reads an object of type <see cref="uint"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        [CLSCompliant(false)]
        public virtual uint ReadUInt32() { return 0; }

        /// <summary>
        /// Reads an object of type <see cref="uint"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        [CLSCompliant(false)]
        public virtual uint ReadUInt32(uint defaultValue) { return 0; }

        /// <summary>
        /// Reads an object of type <see cref="ulong"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <returns>object read from the stream</returns>
        [CLSCompliant(false)]
        public virtual ulong ReadUInt64() { return 0; }

        /// <summary>
        /// Reads an object of type <see cref="ulong"/> from the current stream 
        /// and advances the stream position. 
        /// This method reads directly from the underlying stream.
        /// </summary>
        /// <param name="defaultValue">Default value to be used for deserializing old version of the object</param>
        /// <returns>object read from the stream</returns>
        [CLSCompliant(false)]
        public virtual ulong ReadUInt64(ulong defaultValue) { return 0; }

        #endregion

        #region /      CompactBinaryReader.SkipXXX      /

        /// <summary>
        /// Skips an object of type <see cref="bool"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipBoolean();

        /// <summary>
        /// Skips an object of type <see cref="byte"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipByte();

        /// <summary>
        /// Skips an object of type <see cref="byte[]"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        /// <param name="count">number of bytes read</param>
        public abstract void SkipBytes(int count);

        /// <summary>
        /// Skips an object of type <see cref="char"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipChar();

        /// <summary>
        /// Skips an object of type <see cref="char[]"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipChars(int count);

        /// <summary>
        /// Skips an object of type <see cref="decimal"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipDecimal();

        /// <summary>
        /// Skips an object of type <see cref="float"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipSingle();

        /// <summary>
        /// Skips an object of type <see cref="double"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipDouble();

        /// <summary>
        /// Skips an object of type <see cref="short"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipInt16();

        /// <summary>
        /// Skips an object of type <see cref="int"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipInt32();

        /// <summary>
        /// Skips an object of type <see cref="long"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipInt64();

        /// <summary>
        /// Skips an object of type <see cref="string"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipString();

        /// <summary>
        /// Skips an object of type <see cref="DateTime"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipDateTime();

        /// <summary>
        /// Skips an object of type <see cref="Guid"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        public abstract void SkipGuid();

        /// <summary>
        /// Skips an object of type <see cref="sbyte"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        [CLSCompliant(false)]
        public virtual void SkipSByte() { }

        /// <summary>
        /// Skips an object of type <see cref="ushort"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        [CLSCompliant(false)]
        public virtual void SkipUInt16() { }

        /// <summary>
        /// Skips an object of type <see cref="uint"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        [CLSCompliant(false)]
        public virtual void SkipUInt32() { }

        /// <summary>
        /// Skips an object of type <see cref="ulong"/> from the current stream 
        /// and advances the stream position. 
        /// This method Skips directly from the underlying stream.
        /// </summary>
        [CLSCompliant(false)]
        public virtual void SkipUInt64() { }

        #endregion
    }
}


