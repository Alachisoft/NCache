// Copyright (c) 2017 Alachisoft
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
// limitations under the License.

using System;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// Thrown whenever one of the data partition goes down
    /// </summary>
    [Serializable]
    public class InvalidReaderException : OperationFailedException
    {
        /// <summary> 
        /// default constructor. 
        /// </summary>
        public InvalidReaderException() { }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public InvalidReaderException(string reason)
            : base(reason, false)
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public InvalidReaderException(string reason, Exception inner)
            : base(reason, inner, false)
        {
        }
    }
}
