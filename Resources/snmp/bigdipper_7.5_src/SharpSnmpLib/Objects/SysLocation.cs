// sysLocation class.
// Copyright (C) 2009-2010 Lex Li
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using Lextm.SharpSnmpLib.Pipeline;

namespace Lextm.SharpSnmpLib.Objects
{
    /// <summary>
    /// sysLocation object.
    /// </summary>
    public sealed class SysLocation : ScalarObject
    {
        private OctetString _location = OctetString.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="SysLocation"/> class.
        /// </summary>
        public SysLocation()
            : base(new ObjectIdentifier("1.3.6.1.2.1.1.6.0"))
        {
        }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public override ISnmpData Data
        {
            get 
            { 
                return _location; 
            }
            
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                
                if (value.TypeCode != SnmpType.OctetString)
                {
                    throw new ArgumentException("wrong data type", "value");
                }

                _location = (OctetString)value;
            }
        }
    }
}
