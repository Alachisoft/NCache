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
    ///  This exception is thrown whenever Attribute index is not found in case of NamedTags
    /// </summary>
    /// <example>The following example demonstrates how to use this exception in your code.
    /// <code>
    /// 
    /// try
    /// {
    ///	    ...
    /// }
    /// catch(AttributeIndexNotDefined ex)
    /// {
    ///     ...
    /// }
    /// 
    /// </code>
    /// </example>
    [Serializable]
    public class AttributeIndexNotDefined : Exception, ISerializable
    {
        /// <summary>
        /// Constructor that takes error as argument.
        /// </summary>
        /// <param name="error"></param>
        public AttributeIndexNotDefined(String error)
            : base(error)
        {
        }

        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="error"></param>
        /// <param name="exception"></param>
        public AttributeIndexNotDefined(String error, Exception exception)
            : base(error, exception)
        {
        }

        /// <summary>
        /// Constructor that take serialization info and streaming context.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public AttributeIndexNotDefined(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}