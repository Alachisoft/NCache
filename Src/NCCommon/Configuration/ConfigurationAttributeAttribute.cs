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

namespace Alachisoft.NCache.Common.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigurationAttributeAttribute : ConfigurationAttributeBase
    {
        private string _attributeName;
        private string _appendedText = "";
        private bool _writeIfNull;

        public ConfigurationAttributeAttribute(string attribName)
            : this(attribName,true)
        {
            _attributeName = attribName;
        }

        public ConfigurationAttributeAttribute(string attribName, string appendedText)
            : this(attribName, false, false, appendedText)
        {
            _attributeName = attribName;
        }

        public ConfigurationAttributeAttribute(string attribName, bool isRequired, bool isCollection, string appendedText)
            : this(attribName, false, false, appendedText,true)
        {

        }

        public ConfigurationAttributeAttribute(string attribName, bool writeIfNull)
            : base(false, false)
        {
            _attributeName = attribName;
            _writeIfNull = writeIfNull;
        }

        public ConfigurationAttributeAttribute(string attribName, bool isRequired, bool isCollection, string appendedText, bool writeIfNull)
            : base(isRequired, false)
        {
            _attributeName = attribName;
            _appendedText = appendedText;
            _writeIfNull = writeIfNull;
        }

        /// <summary>
        /// Gets the attribute name.
        /// </summary>
        public string AttributeName
        {
            get { return _attributeName; }
        }

        /// <summary>
        /// Gets/sets the appended text.
        /// <remarks>A property value may have some appended extra text just for
        /// describtion of the property e.g. size="250mb". Here 250 is the size
        /// and mb is just for describtion that size is in mbs.</remarks>
        /// </summary>
        public string AppendedText
        {
            get { return _appendedText; }
        }


        /// <summary>
        /// Indicates whether a confiugration section/attribute is to be written if null.
        /// </summary>
        public bool WriteIfNull
        {
            get { return _writeIfNull; }
        }
    }
}