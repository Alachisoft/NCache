// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License

using System;
using System.Security.Permissions;
using System.Runtime.Serialization;

///<summary>
/// Different exceptions that are likely to occur in the client applications
/// are defined in this namespace. These exceptions can be caught in the client applications
/// and properly dealt with once this namespace is referenced.
///</summary>

namespace Alachisoft.NCache.Runtime.Exceptions

{

    /// <summary>
    /// It is the base class for all the exceptions that are thrown from NCache. 
    /// So you can catch this exception for all the exceptions thrown from within the NCache.
    /// </summary>
    /// <example>The following example demonstrates how to use this exception in your code.
    /// <code>
    /// 
    /// try
    /// {
    ///	    ...
    /// }
    /// catch(CacheException ex)
    /// {
    ///     ...
    /// }
    /// 
    /// </code>
    /// </example>
    [Serializable]
    public class CacheException : Exception
    {
        
        /// <summary> 
        /// default constructor. 
        /// </summary>
        public CacheException() { }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public CacheException(string reason)
            : base(reason)
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public CacheException(string reason, Exception inner)
            : base(reason, inner)
        {
        }

      

        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// overloaded constructor, manual serialization. 
        /// </summary>
        protected CacheException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// manual serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion


    }
}

