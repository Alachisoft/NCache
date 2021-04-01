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
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

using Alachisoft.NCache.IO;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Serialization.Surrogates
{
    /// <summary>
    /// A serialization surrogate in the sense that it is responsible for serialization and
    /// deserialization of another object. 
    /// </summary>
    public class SerializationSurrogate : ISerializationSurrogate
    {
        private short handle;
        private Type type;
        private short subTypeHandle;
        
        public IObjectPool ObjectPool
        {
            get; set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="t">The type for which it is a surrogate</param>
        public SerializationSurrogate(Type t, IObjectPool pool)
        {
            type = t;
            ObjectPool = pool;
        }

        /// <summary> 
        /// Return the type of object for which this object is a surrogate. 
        /// </summary>
        public Type ActualType
        {
            get { return type; }
        }

        /// <summary> 
        /// Magic ID associated with the type provided by the <see cref="TypeSurrogateSelector"/> 
        /// </summary>
        public short TypeHandle
        {
            get { return handle; }
            set { handle = value; }
        }

        /// <summary>
        /// Sub Type handle associated with the type provided by the <see cref="TypeSurrogateSelector"/> 
        /// </summary>
        public short SubTypeHandle
        {
            get { return subTypeHandle; }
            set { subTypeHandle = value; }
        }

        public virtual bool VersionCompatible { get { return false; } }

        /// <summary>
        /// Creates instance of <see cref="ActualType"/>. Calls the default constructor and returns
        /// the object. There must be a default constructor even though it is private.
        /// </summary>
        /// <returns></returns>
        public virtual object CreateInstance()
        {
            return Activator.CreateInstance(ActualType,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.Instance,
                null, null, null, null);
        }
        /// <summary>
        /// Creates instance of <see cref="ActualType"/>. Calls the default constructor and returns
        /// the object. There must be a default constructor even though it is private.
        /// </summary>
        /// <returns></returns>
        public virtual object GetInstance(MemoryManager objManager)
        {
            object obj = null;

            if (objManager != null)
            {
                ObjectProvider provider = objManager.GetProvider(ActualType);
                if (provider != null)
                {
                    //Trace.error("Deserialization.GetInstance",ActualType + " being rented");
                    obj = provider.RentAnObject();
                }
                //else
                    //Trace.error("Deserialization.GetInstance",ActualType + " no rented available");
            }
            return obj;
        }
        /// <summary>
        /// Read an object of type <see cref="ActualType"/> from the stream reader
        /// </summary>
        /// <param name="reader">stream reader</param>
        /// <returns>object read from the stream reader</returns>
        public virtual object Read(CompactBinaryReader reader)
        {
            return null;
        }

        /// <summary>
        /// Write an object of type <see cref="ActualType"/> to the stream writer
        /// </summary>
        /// <param name="writer">stream writer</param>
        /// <param name="graph">object to be written to the stream reader</param>
        public virtual void Write(CompactBinaryWriter writer, object graph)
        {
        }

        /// <summary>
        /// Skip bytes as written by this surrogate and advances the current stream position
        /// </summary>
        /// <param name="reader">stream reader</param>
        public virtual void Skip(CompactBinaryReader reader)
        {
        }
    }
}
