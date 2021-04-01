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
using System.Collections.Generic;
using System.Text;

using Alachisoft.NCache.Common.Configuration;


namespace Alachisoft.NCache.Tools.Common
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgumentAttribute : ConfigurationAttributeBase
    {
        private string _shortNotation = "";
        private string _shortNotation2 = "";
        private string _fullName = "";
        private string _fullName2 = "";
        private string _appendedText = "";
        private object _defaultValue = "";

        public ArgumentAttribute(string attribName, string attribName2)
            : base(false, false)
        {
            _shortNotation = attribName;
            _shortNotation2 = attribName2;
        }

        public ArgumentAttribute(string attribName1, string attribName2, string attribName3, string attribName4)
            :base(false, false)
        {
            _shortNotation = attribName1;
            _fullName = attribName2;
            _shortNotation2 = attribName3;
            _fullName2 = attribName4;
        }

        public ArgumentAttribute(string attribName1, string attribName2, bool isRequired, bool isCollection, string appendedText)
            : base(isRequired, false)
        {
            _shortNotation = attribName1;
            _shortNotation2 = attribName2;
            _appendedText = appendedText;
        }

        public ArgumentAttribute(string attribName1, string attribName2, object defaultValue)
            : this(attribName1, attribName2, false, false, "")
        {
            _shortNotation = attribName1;
            _shortNotation2 = attribName2;
            _defaultValue = defaultValue;
        }

        public ArgumentAttribute(string attribName1, string attribName2, string attribName3, string attribName4, object defaultValue)
            : this(attribName1, attribName3, false, false, "")
        {
            _shortNotation = attribName1;
            _shortNotation2 = attribName3;
            _fullName = attribName2;
            _fullName2 = attribName4;
            _defaultValue = defaultValue;
        }

        /// <summary>
        /// Gets the attribute name.
        /// </summary>
        public string ShortNotation
        {
            get { return _shortNotation; }
        }

        public string FullName
        {
            get { return _fullName; }
        }

        public string ShortNotation2
        {
            get { return _shortNotation2; }
        }

        public string FullName2
        {
            get { return _fullName2; }
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

        public object DefaultValue
        {
            get { return _defaultValue; }
        }
    }
}
