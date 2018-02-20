// ==++==
// 
//   Copyright (c). 2015. Microsoft Corporation.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ==--==


using System;
using System.Collections;
using System.Runtime;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    public class CompatibleComparer : IEqualityComparer
    {
        private IComparer _comparer;
        private IHashCodeProvider _hcp;
        internal IComparer Comparer
        {
#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
#endif
            get
            {
                return this._comparer;
            }
        }
        internal IHashCodeProvider HashCodeProvider
        {
#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
#endif
            get
            {
                return this._hcp;
            }
        }
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
#endif
        internal CompatibleComparer(IComparer comparer, IHashCodeProvider hashCodeProvider)
        {
            this._comparer = comparer;
            this._hcp = hashCodeProvider;
        }
        public int Compare(object a, object b)
        {
            if (a == b)
            {
                return 0;
            }
            if (a == null)
            {
                return -1;
            }
            if (b == null)
            {
                return 1;
            }
            if (this._comparer != null)
            {
                return this._comparer.Compare(a, b);
            }
            IComparable comparable = a as IComparable;
            if (comparable != null)
            {
                return comparable.CompareTo(b);
            }
            throw new ArgumentException(ResourceHelper.GetResourceString("Argument_ImplementIComparable"));
        }
        public new bool Equals(object a, object b)
        {
            return this.Compare(a, b) == 0;
        }
        public int GetHashCode(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (this._hcp != null)
            {
                return this._hcp.GetHashCode(obj);
            }
            return obj.GetHashCode();
        }
    }
}