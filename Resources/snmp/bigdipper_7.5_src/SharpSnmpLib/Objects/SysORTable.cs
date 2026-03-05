// sysORTable class.
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

using System.Collections.Generic;
using Lextm.SharpSnmpLib.Pipeline;

namespace Lextm.SharpSnmpLib.Objects
{
    /// <summary>
    /// sysORTable object.
    /// </summary>
    public sealed class SysORTable : TableObject
    {
        // "1.3.6.1.2.1.1.9.1"
        private readonly IList<ScalarObject> _elements = new List<ScalarObject>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SysORTable"/> class.
        /// </summary>
        public SysORTable()
        {
            _elements.Add(new SysORIndex(1));
            _elements.Add(new SysORIndex(2));
            _elements.Add(new SysORID(1, new ObjectIdentifier("1.3")));
            _elements.Add(new SysORID(2, new ObjectIdentifier("1.4")));
            _elements.Add(new SysORDescr(1, new OctetString("Test1")));
            _elements.Add(new SysORDescr(2, new OctetString("Test2")));
            _elements.Add(new SysORUpTime(1, new TimeTicks(1)));
            _elements.Add(new SysORUpTime(2, new TimeTicks(2)));
        }

        /// <summary>
        /// Gets the objects in the table.
        /// </summary>
        /// <value>The objects.</value>
        protected override IEnumerable<ScalarObject> Objects
        {
            get { return _elements; }
        }
    }
}