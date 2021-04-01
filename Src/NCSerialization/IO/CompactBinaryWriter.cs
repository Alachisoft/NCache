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
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.IO
{
    /// <summary>
    /// This class encapsulates a <see cref="BinaryWriter"/> object. It also provides an extra
    /// Write method for <see cref="System.Object"/> types. 
    /// </summary>
    public class CompactBinaryWriter : CompactWriter, IDisposable
    {
        private SerializationContext context;
        private BinaryWriter writer;

        /// <summary>
        /// Constructs a compact writer over a <see cref="Stream"/> object.
        /// </summary>
        /// <param name="output"><see cref="Stream"/> object</param>
        public CompactBinaryWriter(Stream output)
            : this(output, new UTF8Encoding(true))
        {
        }
        /// <summary>
        /// Constructs a compact writer over a <see cref="Stream"/> object.
        /// </summary>
        /// <param name="output"><see cref="Stream"/> object</param>
        /// <param name="encoding"><see cref="Encoding"/> object</param>
        public CompactBinaryWriter(Stream output, Encoding encoding)
        {
            context = new SerializationContext();
            writer = new BinaryWriter(output, encoding);
        }

        /// <summary> Returns the underlying <see cref="BinaryWriter"/> object. </summary>
        internal BinaryWriter BaseWriter { get { return writer; } }
        /// <summary> Returns the current <see cref="SerializationContext"/> object. </summary>
        internal SerializationContext Context { get { return context; } }

        /// <summary>
        /// Close the underlying <see cref="BinaryWriter"/>.
        /// </summary>
        public void Dispose()
        {
            if (writer != null) writer.Close();
        }
        /// <summary>
        /// Close the underlying <see cref="BinaryWriter"/>.
        /// </summary>
        public void Dispose(bool closeStream)
        {
            if (closeStream) writer.Close();
            writer = null;
        }

        public override Stream BaseStream { get { return writer.BaseStream; } }
        /// <summary>
        /// Writes <paramref name="graph"/> to the current stream and advances the stream position. 
        /// </summary>
        /// <param name="graph">Object to write</param>
        public override void WriteObject(object graph)
        {
            //Console.WriteLine(graph);

            // Find an appropriate surrogate for the object
            ISerializationSurrogate surrogate = TypeSurrogateSelector.GetSurrogateForObject(graph, context.CacheContext);
            // write type handle
            writer.Write(surrogate.TypeHandle);
            try
            {
                surrogate.Write(this, graph);
            }
            catch (CompactSerializationException)
            {
                throw;
            }
            catch (System.Threading.ThreadAbortException) 
            {
                throw;
            }
            catch (System.Threading.ThreadInterruptedException)
            {
                throw;
            }
            catch (System.Runtime.Serialization.SerializationException ex)
            {
                if (ex.Message.Contains("is not marked as serializable"))
                    throw new CompactSerializationException(graph.GetType().FullName + " is not marked as serializable.", ex);
                else
                    throw new CompactSerializationException(ex.Message);
            }
            catch (Exception e)
            {
                //Trace.error("CompactBinaryWriter.WriteObject", "type: " + surrogate.ActualType + " handle: " + surrogate.TypeHandle
                //    + "exception : " + e);
                throw new CompactSerializationException(e.Message);
            }
        }

        public override void WriteObjectAs<T>(T graph)
        {
            if (graph == null)
                throw new ArgumentNullException("graph");

            // Find an appropriate surrogate for the object
            ISerializationSurrogate surrogate = TypeSurrogateSelector.GetSurrogateForType(typeof(T), context.CacheContext);
            surrogate.Write(this, graph);
        }

        public string CacheContext
        {
            get { return context.CacheContext; }
        }

        #region /      CompactBinaryWriter.Write(XXX)      /

        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public override void Write(bool value) { writer.Write(value); }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public override void Write(byte value) { writer.Write(value); }
        /// <summary>
        /// Writes <paramref name="ch"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="ch">Object to write</param>
        public override void Write(char ch) { writer.Write(ch); }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public override void Write(short value) { writer.Write(value); }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public override void Write(int value) { writer.Write(value); }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public override void Write(long value) { writer.Write(value); }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public override void Write(decimal value) { writer.Write(value); }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public override void Write(float value) { writer.Write(value); }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public override void Write(double value) { writer.Write(value); }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public override void Write(DateTime value) { writer.Write(value.Ticks); }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public override void Write(Guid value) { writer.Write(value.ToByteArray()); }
        /// <summary>
        /// Writes <paramref name="buffer"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="buffer">Object to write</param>
        public override void Write(byte[] buffer)
        {
            if (buffer != null)
                writer.Write(buffer);
            else
                WriteObject(null);
        }
        /// <summary>
        /// Writes <paramref name="chars"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="chars">Object to write</param>
        public override void Write(char[] chars)
        {
            if (chars != null)
                writer.Write(chars);
            else
                WriteObject(null);
        }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        public override void Write(string value)
        {
            if (value != null)
                writer.Write(value);
            else
                WriteObject(null);
        }
        /// <summary>
        /// Writes <paramref name="buffer"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="buffer">buffer to write</param>
        /// <param name="index">starting position in the buffer</param>
        /// <param name="count">number of bytes to write</param>
        public override void Write(byte[] buffer, int index, int count)
        {
            if (buffer != null)
                writer.Write(buffer, index, count);
            else
                WriteObject(null);
        }
        /// <summary>
        /// Writes <paramref name="chars"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="chars">buffer to write</param>
        /// <param name="index">starting position in the buffer</param>
        /// <param name="count">number of bytes to write</param>
        public override void Write(char[] chars, int index, int count)
        {
            if (chars != null)
                writer.Write(chars, index, count);
            else
                WriteObject(null);
        }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        [CLSCompliant(false)]
        public override void Write(sbyte value) { writer.Write(value); }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        [CLSCompliant(false)]
        public override void Write(ushort value) { writer.Write(value); }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        [CLSCompliant(false)]
        public override void Write(uint value) { writer.Write(value); }
        /// <summary>
        /// Writes <paramref name="value"/> to the current stream and advances the stream position. 
        /// This method writes directly to the underlying stream.
        /// </summary>
        /// <param name="value">Object to write</param>
        [CLSCompliant(false)]
        public override void Write(ulong value) { writer.Write(value); }

        #endregion
    }
}
