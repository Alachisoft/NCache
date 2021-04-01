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
using System.Collections;
using System.IO;

using Alachisoft.NCache.IO;
using Alachisoft.NCache.Serialization.Surrogates;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Serialization.Formatters
{
	/// <summary>
	/// Serializes and deserializes an object, or an entire graph of connected objects, in binary format.
	/// Uses the compact serialization framework to achieve better stream size and cpu time utlization.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The basic idea behind space conservation is that every 'known type' is assigned a 2-byte 
	/// type handle by the system. Native .NET serialization stores the complete type information
	/// with serialized object data, which includes assembly, version and tokens etc. Instead of storing
	/// such information only a type handle is stored, which lets the system uniquely identify 'known types'.
	/// 
	/// A known type is a type that is registered with the <see cref="TypeSurrogateProvider"/>. Moreover
	/// surrogate types take care of serializing only the required information. Information related to fields
	/// and attributes is not stored as in case of native serialization.
	/// </para>
	/// <para>
	/// From performance's perspective reflection is avoided by using surrogates for types. A type surrogate
	/// is intimate with the internals of a type and therefore does not need reflection to guess 
	/// object schema.
	/// </para>
	/// For types that are not known to the system the formatter reverts to the default .NET 
	/// serialization scheme.
	/// </remarks>
	public class CompactBinaryFormatter
	{
		/// <summary>
		/// Serializes an object and returns its binary representation.
		/// </summary>
		/// <param name="graph">object to serialize</param>
		/// <returns>binary form of object</returns>
		static public byte[] ToByteBuffer(object graph,string cacheContext)
{
			using(MemoryStream stream = new MemoryStream())
			{
				Serialize(stream, graph,cacheContext);
				return stream.ToArray();
			}
		}		

		/// <summary>
		/// Deserializes the binary representation of an object.
		/// </summary>
		/// <param name="buffer">binary representation of the object</param>
		/// <returns>deserialized object</returns>
		static public object FromByteBuffer(byte[] buffer,string cacheContext, System.Runtime.Serialization.SerializationBinder binder = null)
        {
			using(MemoryStream stream = new MemoryStream(buffer))
			{
				return Deserialize(stream,cacheContext,binder);
			}
		}

		/// <summary>
		/// Serializes an object into the specified stream.
		/// </summary>
		/// <param name="stream">specified stream</param>
		/// <param name="graph">object</param>
		static public void Serialize(Stream stream, object graph,string cacheContext)
		{
			using(CompactBinaryWriter writer = new CompactBinaryWriter(stream))
			{
				Serialize(writer, graph,cacheContext);
			}
		}

        /// <summary>
        /// Serializes an object into the specified stream.
        /// </summary>
        /// <param name="stream">specified stream</param>
        /// <param name="graph">object</param>
        static public void Serialize(Stream stream, object graph, string cacheContext,bool closeStream)
        {
            CompactBinaryWriter writer = new CompactBinaryWriter(stream);
            Serialize(writer, graph, cacheContext);
            writer.Dispose(closeStream);
        }
        /// <summary>
        /// Serializes an object into the specified stream.
        /// </summary>
        /// <param name="stream">specified stream</param>
        /// <param name="graph">object</param>
        static public void Serialize(Stream stream, object graph, string cacheContext, bool closeStream,MemoryManager objManager)
        {
            CompactBinaryWriter writer = new CompactBinaryWriter(stream);
            writer.Context.MemManager = objManager;
            Serialize(writer, graph, cacheContext);
            writer.Dispose(closeStream);
        }
		/// <summary>
		/// Deserializes an object from the specified stream.
		/// </summary>
		/// <param name="stream">specified stream</param>
		/// <returns>deserialized object</returns>
		static public object Deserialize(Stream stream,string cacheContext, System.Runtime.Serialization.SerializationBinder binder = null)
		{
			using(CompactBinaryReader reader = new CompactBinaryReader(stream))
			{
                reader.Context.Binder = binder;
				return Deserialize(reader,cacheContext, false);
			}
		}
        /// <summary>
        /// Deserializes an object from the specified stream.
        /// </summary>
        /// <param name="stream">specified stream</param>
        /// <returns>deserialized object</returns>
        static public object Deserialize(Stream stream, string cacheContext, bool closeStream)
        {
            object obj;
            CompactBinaryReader reader = new CompactBinaryReader(stream);
            obj = Deserialize(reader, cacheContext, false);
            reader.Dispose(closeStream);
            return obj;
        }
        /// <summary>
        /// Deserializes an object from the specified stream.
        /// </summary>
        /// <param name="stream">specified stream</param>
        /// <returns>deserialized object</returns>
        static public object Deserialize(Stream stream, string cacheContext, bool closeStream,MemoryManager memManager)
        {
            object obj;
            CompactBinaryReader reader = new CompactBinaryReader(stream);
            reader.Context.MemManager = memManager;
            obj = Deserialize(reader, cacheContext, false);
            reader.Dispose(closeStream);
            return obj;
        }
		/// <summary>
		/// Serializes an object into the specified compact binary writer.
		/// </summary>
		/// <param name="writer">specified compact binary writer</param>
		/// <param name="graph">object</param>
		static internal void Serialize(CompactBinaryWriter writer, object graph,string cacheContext)
		{
			// Find an appropriate surrogate for the object
            ISerializationSurrogate surrogate =
                TypeSurrogateSelector.GetSurrogateForObject(graph,cacheContext);
			// write type handle
			writer.Context.CacheContext = cacheContext;
			writer.Write(surrogate.TypeHandle);
			surrogate.Write(writer, graph);
		}
       
		/// <summary>
		/// Deserializes an object from the specified compact binary writer.
		/// </summary>
        /// <param name="reader">Stream containing reader</param>
        /// <param name="cacheContext">Name of the cache</param>
        /// <param name="skip">True to skip the bytes returning null</param>
		static internal object Deserialize(CompactBinaryReader reader,string cacheContext, bool skip)
		{
			// read type handle
			short handle = reader.ReadInt16();
			reader.Context.CacheContext = cacheContext;
			// Find an appropriate surrogate by handle
			ISerializationSurrogate surrogate =
                TypeSurrogateSelector.GetSurrogateForTypeHandle(handle,cacheContext);
            
            if (surrogate == null)
            {
                surrogate = TypeSurrogateSelector.GetSurrogateForSubTypeHandle(handle, reader.ReadInt16(), cacheContext);
            }

			if(surrogate == null)
			{
				throw new CompactSerializationException("Type handle " + handle + "is not registered with Compact Serialization Framework");
			}
            if (!skip)
            {
                return surrogate.Read(reader);
            }
            else
            {
                surrogate.Skip(reader);
                return null;
            }
		}
	}
}