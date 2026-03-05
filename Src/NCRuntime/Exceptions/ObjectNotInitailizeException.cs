//  Copyright (c) 2026 Alachisoft
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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// This exception is thrown whenever the object not initailized.
    /// </summary>
    [Serializable]
    public class ObjectNotInitializedException : Exception
    {

        /// <summary> 
        /// Default constructor for this class. 
        /// </summary>
        public ObjectNotInitializedException()
        {
        }

        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="reason">Exception Message</param>
        public ObjectNotInitializedException( string message)
           : base(message)
        {
        }

       
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="reason">Exception message</param>
        /// <param name="inner">nested exception</param>
        public ObjectNotInitializedException( string message, Exception inner)
           : base(message, inner)
        {
        }
        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// Overloaded constructor, manual serialization. 
        /// </summary>
        protected ObjectNotInitializedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Manual serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
           base.GetObjectData(info, context);
        }

        #endregion
    }
}