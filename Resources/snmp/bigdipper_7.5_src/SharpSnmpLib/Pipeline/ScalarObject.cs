// Scalar object class.
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

using System.Globalization;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// Scalar object interface.
    /// </summary>
    public abstract class ScalarObject : SnmpObjectBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarObject"/> class.
        /// </summary>
        /// <param name="id">The ID.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        protected ScalarObject(ObjectIdentifier id)
        {
            Id = id;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarObject"/> class.
        /// </summary>
        /// <param name="dots">The ID string.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        protected ScalarObject(string dots)
            : this(new ObjectIdentifier(dots))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarObject"/> class.
        /// </summary>
        /// <param name="dots">The ID string.</param>
        /// <param name="index">The index.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        protected ScalarObject(string dots, int index)
            : this(string.Format(CultureInfo.InvariantCulture, dots, index))
        {
        }

        /// <summary>
        /// Gets the variable.
        /// </summary>
        /// <value>The variable.</value>
        public Variable Variable
        {
            get { return new Variable(Id, Data); }
        }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public abstract ISnmpData Data
        {
            get; set;
        }

        /// <summary>
        /// Gets the ID.
        /// </summary>
        /// <value>The ID.</value>
        private ObjectIdentifier Id { get; set; }

        /// <summary>
        /// Matches the GET NEXT criteria.
        /// </summary>
        /// <param name="id">The ID in GET NEXT message.</param>
        /// <returns><c>null</c> if it does not match.</returns>
        public override ScalarObject MatchGetNext(ObjectIdentifier id)
        {
            return Id > id ? this : null;
        }

        /// <summary>
        /// Matches the GET criteria.
        /// </summary>
        /// <param name="id">The ID in GET message.</param>
        /// <returns><c>null</c> if it does not match.</returns>
        public override ScalarObject MatchGet(ObjectIdentifier id)
        {
            return Id == id ? this : null;
        }
    }
}
