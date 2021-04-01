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

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Provides the methods for custom attribute specification
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    internal class TargetMethodAttribute : Attribute
    {
        int _overload;
        [ThreadStaticAttribute]
        private static int _sMethodOverload = 0;

        /// <summary>
        /// Constructor which sets the value of specified overload
        /// </summary>
        /// <param name="overload"></param>
        /// <returns>Int</returns>
        public TargetMethodAttribute(int overload)
        {
            this._overload = overload;
        }

        /// <summary>
        /// Gets the value of overload specified
        /// </summary>
        /// <returns>Int</returns>
        public int Overload
        {
            get { return _overload; }
        }

        /// <summary>
        /// Gets/Sets the value of method overload specified
        /// </summary>
        /// <returns>Int</returns>
        public static int MethodOverload
        {
            set { _sMethodOverload = value; }
            get { return _sMethodOverload; }
        }
    }
}
