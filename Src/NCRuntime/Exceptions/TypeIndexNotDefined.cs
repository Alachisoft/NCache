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
using System.Runtime.Serialization;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// This exception is thrown whenever Type Index is not found in case of NamedTags.
    /// </summary>
    [Serializable]
    public class TypeIndexNotDefined : Exception, ISerializable
    {
        /// <summary>
        /// Overloaded constructor that takes error message as an argument.
        /// </summary>
        /// <param name="error"></param>
        public TypeIndexNotDefined(String error)
            : base(error)
        {
        }

        /// <summary>
        /// Overloaded constructor that take error message and exception as arguments.
        /// </summary>
        /// <param name="error"></param>
        /// <param name="exception"></param>
        public TypeIndexNotDefined(String error, Exception exception)
            : base(error, exception)
        {
        }

        /// <summary>
        /// Overloaded constructor that take info and context as arguments.
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        public TypeIndexNotDefined(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}