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
using System.Collections;
using System.Text;

namespace Alachisoft.NCache.Common.Configuration
{
    public abstract class ConfigurationAttributeBase : Attribute
    {
        private bool _isRequired;
        private bool _isCollection;
        

        public ConfigurationAttributeBase(bool isRequired, bool isCollection)
        {
            _isRequired = isRequired;
            _isCollection = isCollection;
        }
        

        /// <summary>
        /// Indicates whether a confiugration section/attribute is required or not.
        /// </summary>
        public bool IsRequired
        {
            get { return _isRequired; }
        }
        
    }
}
