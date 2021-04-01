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

namespace Alachisoft.NCache.Common.RPCFramework
{
    /// <summary>
    /// Identifies
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TargetMethodAttribute : Attribute
    {
        private string _methodName;
        private int _overLoad;

        public TargetMethodAttribute(string methodName):base()
        {
            this.Method = methodName;
            this.Overload = 1;
        }

        public TargetMethodAttribute(string methodName, int overload):base()
        {
            this.Method = methodName;
            this.Overload = overload;
        }

        /// <summary>
        /// Gets/Sets the Name of the target method to be invoked by remote client
        /// </summary>
        public string Method
        {
            get { return _methodName; }
            set { _methodName = value; }
        }

        /// <summary>
        /// Gets/Sets the overload number of an overloaded method to be invoked by remote client
        /// </summary>
        public int Overload
        {
            get { return _overLoad; }
            set { _overLoad = value; }
        }
    }
}
