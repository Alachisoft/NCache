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
using System.Net;
using System.Collections;
using System.Reflection;
//using System.Web.SessionState;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using Alachisoft.NCache.IO;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;
using System.Web;
using Alachisoft.NCache.Runtime.Serialization.IO;


namespace Alachisoft.NCache.Serialization.Surrogates
{
    

    /// <summary>
    /// A surrogate for value types that uses delegates for readig and writing objects. Delegates can be
    /// user defined or automatically generated.
    /// </summary>
    /// <remarks>
    /// Reader and writer delegates can be provided at runtime. If no user defined delegates are
    /// specified the surrogate generates delegates at runtime for reading and writing the specified
    /// type.
    /// </remarks>
    /// <typeparam name="T">Value type for which it is a surrogate</typeparam>
    public class DynamicValueTypeSurrogate<T> : SerializationSurrogate
                                         where T : struct
    {
        /// <summary>
        /// Instance of <see cref="DefaultConstructorDelegate"/> delegate used for instantiating objects
        /// </summary>
        protected DefaultConstructorDelegate mNewMtd;
        /// <summary>
        /// Instance of <see cref="ReadObjectDelegate"/> delegate used for deserializing objects
        /// </summary>
        protected ReadObjectDelegate mReadMtd;
        /// <summary>
        /// Instance of <see cref="WriteObjectDelegate"/> delegate used for serializing objects
        /// </summary>
        protected WriteObjectDelegate mWriteMtd;

        /// <summary>
        /// Default constructor 
        /// </summary>
        /// <remarks>
        /// This constructor uses Reflection.Emit to generate default reader and writer delegates for
        /// serialization of specified type.
        /// </remarks>
        public DynamicValueTypeSurrogate() : base(typeof(T), null)
        {
            CommonConstruct();
            mReadMtd = DynamicSurrogateBuilder.CreateReaderDelegate(typeof(T));
            mWriteMtd = DynamicSurrogateBuilder.CreateWriterDelegate(typeof(T));
        }

        /// <summary>
        /// Overloaded constructor, uses user specified delegates
        /// </summary>
        /// <param name="read">A <see cref="ReadObjectDelegate"/> object</param>
        /// <param name="write">A <see cref="WriteObjectDelegate"/> object</param>
        public DynamicValueTypeSurrogate(ReadObjectDelegate read, WriteObjectDelegate write)
            : base(typeof(T), null)
        {
            CommonConstruct();
            mReadMtd = read;
            mWriteMtd = write;
        }

        /// <summary>
        /// Common initialization method. Creates an instantiator delegate when the given type if not public.
        /// </summary>
        private void CommonConstruct()
        {
            if (!typeof(T).IsPublic)
            {
                mNewMtd = SurrogateHelper.CreateDefaultConstructorDelegate(typeof(T));
            }
        }

        /// <summary>
        /// Read an object of type <see cref="SerializationSurrogate.ActualType"/> from the stream reader
        /// </summary>
        /// <param name="reader">The reader from which the data is deserialized</param>
        /// <returns>object read from the stream reader</returns>
        public override object Read(CompactBinaryReader reader)
        {
            if (typeof(T).IsPublic)
            {
                mReadMtd(reader, default(T));
            }
            return mReadMtd(reader, mNewMtd());
        }

        /// <summary>
        /// Write an object of type <see cref="SerializationSurrogate.ActualType"/> to the stream writer
        /// </summary>
        /// <param name="writer">The writer onto which the specified graph is serialized</param>
        /// <param name="graph">The root of the object graph to be serialized</param>
        public override void Write(CompactBinaryWriter writer, object graph)
        {
            mWriteMtd(writer, graph);
        }
    }
}
