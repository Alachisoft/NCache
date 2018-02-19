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
using Alachisoft.NCache.Common.Configuration;

namespace Alachisoft.NCache.Tools.Common
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgumentAttribute : ConfigurationAttributeBase
    {
        private string _shortNotation = "";
        private string _fullName = "";
        private string _appendedText = "";
        private object _defaultValue = "";

        public ArgumentAttribute(string attribName)
            : base(false, false)
        {
            _shortNotation = attribName;
        }

        public ArgumentAttribute(string attribName1, string attribName2)
            : base(false, false)
        {
            _shortNotation = attribName1;
            _fullName = attribName2;
        }

        public ArgumentAttribute(string attribName, bool isRequired, bool isCollection, string appendedText)
            : base(isRequired, false)
        {
            _shortNotation = attribName;
            _appendedText = appendedText;
        }

        public ArgumentAttribute(string attribName, object defaultValue)
            : this(attribName, false, false, "")
        {
            _shortNotation = attribName;
            _defaultValue = defaultValue;
        }

        public ArgumentAttribute(string attribName, string attribName2, object defaultValue)
            : this(attribName, false, false, "")
        {
            _shortNotation = attribName;
            _fullName = attribName2;
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
