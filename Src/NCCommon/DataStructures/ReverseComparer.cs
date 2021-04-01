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
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.Common.DataStructures
{
    public sealed class ReverseComparer<T> : IComparer<T>
    {
        private readonly IComparer<T> inner;
        public ReverseComparer() : this(null) { }
        public ReverseComparer(IComparer<T> inner)
        {
            this.inner = inner ?? Comparer<T>.Default;
        }
        int IComparer<T>.Compare(T x, T y) { return inner.Compare(y, x); }
    }

    /// <summary>
    /// Null Comparer is for sorting null keys, for now it is being used in orderby to accomodate null values in columns
    /// Null is treated as the smallest
    /// </summary>
    public sealed class NullComparer : IComparer<object>
    {
        public int Compare(object x, object y)
        {
            if (x.GetType() == typeof(NullKey))
                return -1;
            if (y.GetType() == typeof(NullKey))
                return 1;
            return Comparer.Default.Compare(x, y);
        }
    }

    public class NullKey
    {
        //this is an empty class for Null-value keys in dictionary and this is the class used in NullComparer
    }
}