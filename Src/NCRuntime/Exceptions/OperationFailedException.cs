// Copyright (c) 2015 Alachisoft
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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// Thrown whenever an API fails. In case of bulk operation, you even recieve 
    /// information about existing keys or unavailable space wrapped within this exception.
    /// </summary>
    /// <example>The following example demonstrates how to use this exception in your code.
    /// <code>
    /// 
    /// try
    /// {
    ///	    ...
    /// }
    /// catch(OperationFailedException ex)
    /// {
    ///     ...
    /// }
    /// 
    /// </code>
    /// </example>
    [Serializable]
    public class OperationFailedException : CacheException
    {
        private bool _isTracable = true;
        /// <summary> 
        /// default constructor. 
        /// </summary>
        public OperationFailedException()
        {
        }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public OperationFailedException(string reason)
            : base(reason)
        {
        }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public OperationFailedException(string reason, bool isTracable)
            : base(reason)
        {
            this._isTracable = isTracable;
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public OperationFailedException(string reason, Exception inner)
            : base(reason, inner)
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public OperationFailedException(string reason, Exception inner, bool isTracable)
            : base(reason, inner)
        {
            this._isTracable = isTracable;
        }

        /// <exclude/>
        public bool IsTracable
        {
            get { return _isTracable; }
        }

        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// overloaded constructor, manual serialization. 
        /// </summary>
        protected OperationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _isTracable = Convert.ToBoolean(info.GetString("_isTracable"));
        }

        /// <summary>
        /// manual serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_isTracable", _isTracable);
        }

        #endregion
    }
}
