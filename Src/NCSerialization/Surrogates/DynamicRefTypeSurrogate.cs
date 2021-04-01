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

using Alachisoft.NCache.IO;
using System;
using System.Runtime.Serialization;
//using System.Reflection;
namespace Alachisoft.NCache.Serialization.Surrogates
{
    /// <summary>
    /// A surrogate for reference types that uses delegates for readig and writing objects. Delegates can be
    /// user defined or automatically generated.
    /// </summary>
    /// <remarks>
    /// Reader and writer delegates can be provided at runtime. If no user defined delegates are
    /// specified the surrogate generates delegates at runtime for reading and writing the specified
    /// type.
    /// </remarks>
    /// <typeparam name="T">Reference type for which it is a surrogate</typeparam>
    public class DynamicRefTypeSurrogate<T> : SerializationSurrogate
        where T : class
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
        public DynamicRefTypeSurrogate() : base(typeof(T), null)
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
        public DynamicRefTypeSurrogate(ReadObjectDelegate read, WriteObjectDelegate write)
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
        /// Creates instance of <see cref="SerializationSurrogate.ActualType"/>. 
        /// </summary>
        /// <param name="reader">stream reader</param>
        /// <returns>Object that this surrogate must deserialize</returns>
        public override object CreateInstance()
        {
            var type = typeof(T);
            if (type.IsPublic)
            {
                try
                {
                    return System.Activator.CreateInstance<T>();

                }
                catch (MissingMethodException ex)
                {
                    return FormatterServices.GetUninitializedObject(type);
                }

            }
            return mNewMtd();
        }

        /// <summary>
        /// Read an object of type <see cref="SerializationSurrogate.ActualType"/> from the stream reader. 
        /// A fresh instance of the object is passed as parameter.
        /// The surrogate should populate fields in the object from data on the stream
        /// </summary>
        /// <remarks>
        /// This method delegates to the <see cref="ReadObjectDelegate"/> object for reading the object.
        /// </remarks>
        /// <param name="reader">stream reader</param>
        /// <param name="graph">a fresh instance of the object that the surrogate must deserialize</param>
        /// <returns>object read from the stream reader</returns>
        public override object Read(CompactBinaryReader reader)
        {
            if (typeof(T).IsPublic)
            {
                return mReadMtd(reader, (T)CreateInstance());
            }
            return mReadMtd(reader, mNewMtd());
        }

        /// <summary>
        /// Write an object of type <see cref="SerializationSurrogate.ActualType"/> to the stream writer
        /// </summary>
        /// <remarks>
        /// This method delegates to the <see cref="WriteObjectDelegate"/> object for writing the object.
        /// </remarks>
        /// <param name="writer">stream writer</param>
        /// <param name="graph">object to be written to the stream reader</param>
        public override void Write(CompactBinaryWriter writer, object graph)
        {
            //NEVER create a subhandle with 0 if portable 
            if (this.SubTypeHandle > 0)
            {
                writer.Write(this.SubTypeHandle); 
            }
            mWriteMtd(writer, graph);
        }

        /// <summary>
        /// Skip an object of type <see cref="SerializationSurrogate.ActualType"/> from the stream reader. 
        /// A fresh instance of the object is passed as parameter.
        /// The surrogate should populate fields in the object from data on the stream
        /// </summary>
        /// <remarks>
        /// This method delegates to the <see cref="ReadObjectDelegate"/> object for reading the object.
        /// </remarks>
        /// <param name="reader">stream reader</param>
        /// <returns>object read from the stream reader</returns>
        public override void Skip(CompactBinaryReader reader)
        {
            if (typeof(T).IsPublic)
            {
                mReadMtd(reader,(T)CreateInstance());
                return;
            }
            mReadMtd(reader, mNewMtd());
        }
    }
}
