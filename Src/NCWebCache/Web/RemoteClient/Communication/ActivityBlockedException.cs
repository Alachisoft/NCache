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
// limitations under the License.

using System;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Web.Communication
{
    /// <summary>
    /// Thrown whenever an operation is blocked due to node down of server.
    /// </summary>
    /// <example>The following example demonstrates how to use this exception in your code.
    /// <code>
    /// 
    /// try
    /// {
    ///	    ...
    /// }
    /// catch(ActivityBlockedException ex)
    /// {
    ///     ...
    /// }
    /// 
    /// </code>
    /// </example>
    [Serializable]
    internal class ActivityBlockedException : CacheException
    {
        private Address _serverip = null;

        /// <summary> 
        /// default constructor. 
        /// </summary>
        public ActivityBlockedException()
        {
        }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public ActivityBlockedException(string reason)
            : base(reason)
        {
        }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public ActivityBlockedException(string reason, Address blockedServerIp)
            : base(reason)
        {
            this._serverip = blockedServerIp;
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public ActivityBlockedException(string reason, Exception inner)
            : base(reason, inner)
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public ActivityBlockedException(string reason, Exception inner, Address blockedServerIp)
            : base(reason, inner)
        {
            this._serverip = blockedServerIp;
        }

        /// <exclude/>
        public Address BlockedServerIp
        {
            get { return _serverip; }
        }
    }
}