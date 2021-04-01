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

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Hold user provided boolean parameters. If user provides any boolean param like balance nodes
    /// or import hashmap then there is no need to read it from config.
    /// </summary>
    internal sealed class UserProvidedBooleanParameters
    {
        private bool _value;
        private bool _userProvided;

        /// <summary>
        /// Get value of parameter
        /// </summary>
        public bool Value
        {
            get { return this._value; }
            set { this._value = value; }
        }

        /// <summary>
        /// True is passed through parameters, false otherwise (read from config)
        /// </summary>
        public bool UserProvided
        {
            get { return this._userProvided; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="userProvided"></param>
        public UserProvidedBooleanParameters(bool value, bool userProvided)
        {
            this._value = value;
            this._userProvided = userProvided;
        }
    }
}