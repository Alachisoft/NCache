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

namespace Alachisoft.NCache.Common
{
    public class DelegateComparer : DelageComparer<object>, System.Collections.IEqualityComparer
    {
        public new bool Equals(object x, object y)
        {
            return base.Equals(x, y);
        }

        public new int GetHashCode(object obj)
        {
            return base.GetHashCode(obj);
        }
    }

    /// <summary>
    /// This comparer works with proxy delegate classes and makes comparison on actual wrapped delegate instance passed from old API
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DelageComparer<T> : IEqualityComparer<T>
    {
        public bool Equals(T x, T y)
        {
            if (x is Delegate && y is Delegate)
            {
                Delegate xWrppedDelegate = GetWrappedDelegate(x as Delegate);
                Delegate yWrppedDelegate = GetWrappedDelegate(y as Delegate);

                return xWrppedDelegate.Equals(yWrppedDelegate);
            }

            if (x != null && y != null)
                return x.Equals(y);
            else if (x == null && y == null)
                return true;

            return false;
        }

        public int GetHashCode(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (obj is Delegate)
            {
                Delegate dgt = GetWrappedDelegate(obj as Delegate);
                return dgt.GetHashCode();
            }

            return obj.GetHashCode();
        }

        private Delegate GetWrappedDelegate(Delegate delegateInstance)
        {
            if (delegateInstance != null && delegateInstance.Target is IDelegateWrapper)
            {
                IDelegateWrapper wrapper = delegateInstance.Target as IDelegateWrapper;
                return GetWrappedDelegate(wrapper.WrappedDeleage);
            }

            return delegateInstance;
        }
    }



}
