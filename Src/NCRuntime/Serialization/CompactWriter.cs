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

namespace Alachisoft.NCache.Runtime.Serialization.IO
{
    /// <summary>
    /// CompactWriter is the  base class for CompactBinaryWriter.
    /// </summary>

    //[CLSCompliant(false)]
    public abstract class CompactWriter
    {
        /// <summary>
        /// Writes <paramref name="graph"/> to the current stream and advances the stream position. 
        /// </summary>
        /// <param name="graph">Object to write</param>
        public abstract void WriteObject(object graph);

        /// <summary>
        /// Writes the specified type to the current stream and advances the stream position.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="graph"></param>
        public abstract void WriteObjectAs<T>(T graph);

        /// <summary>
        /// Memory stream on which the bytes are written to
        /// </summary>
        public abstract Stream BaseStream { get; }

        #region /      CompactBinaryWriter.Write(XXX)      /

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public abstract void Write(bool value);

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public abstract void Write(byte value);

        /// <summary>
        /// Writes <paramref name="ch"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="ch">Object to write</param>
        public abstract void Write(char ch);

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public abstract void Write(short value);

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public abstract void Write(int value);

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public abstract void Write(long value);

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public abstract void Write(decimal value);

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public abstract void Write(float value);

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public abstract void Write(double value);

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public abstract void Write(DateTime value);

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public abstract void Write(Guid value);

        /// <summary>
        /// Writes <paramref name="buffer"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="buffer">Object to write</param>
        public abstract void Write(byte[] buffer);

        /// <summary>
        /// Writes <paramref name="chars"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="chars">Object to write</param>
        public abstract void Write(char[] chars);

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public abstract void Write(string value);

        /// <summary>
        /// Writes <paramref name="buffer"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="buffer">buffer to write</param>
        /// <param name="index">starting position in the buffer</param>
        /// <param name="count">number of bytes to write</param>
        public abstract void Write(byte[] buffer, int index, int count);

        /// <summary>
        /// Writes <paramref name="chars"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="chars">buffer to write</param>
        /// <param name="index">starting position in the buffer</param>
        /// <param name="count">number of bytes to write</param>
        public abstract void Write(char[] chars, int index, int count);

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        [CLSCompliant(false)]
        public virtual void Write(sbyte value) { }

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        [CLSCompliant(false)]
        public virtual void Write(ushort value) { }

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        [CLSCompliant(false)]
        public virtual void Write(uint value) { }

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        [CLSCompliant(false)]
        public virtual void Write(ulong value) { }

        #endregion
    }
}