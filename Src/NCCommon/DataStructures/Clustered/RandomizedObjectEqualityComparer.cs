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

using System.Linq;
using System.Text;
using System.Security;
using System.Collections;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    public class RandomizedObjectEqualityComparer : IEqualityComparer, IWellKnownStringEqualityComparer
    {
        private long _entropy;
        public RandomizedObjectEqualityComparer()
        {
            this._entropy = HashHelpers.GetEntropy();
        }
        public new bool Equals(object x, object y)
        {
            if (x != null)
            {
                return y != null && x.Equals(y);
            }
            return y == null;
        }

        [SecuritySafeCritical]
        public int GetHashCode(object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            string text = obj as string;
            if (text != null)
            {

#if FEATURE_RANDOMIZED_STRING_HASHING
                return string.InternalMarvin32HashString(text, text.Length, this._entropy);
#endif
            }
            return obj.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            RandomizedObjectEqualityComparer randomizedObjectEqualityComparer = obj as RandomizedObjectEqualityComparer;
            return randomizedObjectEqualityComparer != null && this._entropy == randomizedObjectEqualityComparer._entropy;
        }
        public override int GetHashCode()
        {
            return base.GetType().Name.GetHashCode() ^ (int)(this._entropy & 2147483647L);
        }
        IEqualityComparer IWellKnownStringEqualityComparer.GetRandomizedEqualityComparer()
        {
            return new RandomizedObjectEqualityComparer();
        }
        IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization()
        {
            return null;
        }
    }
}
