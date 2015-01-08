// Copyright (c) 2015 Alachisoft
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
/**
 * 
 * The following code can be found at:
 * http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp01212002.asp
 * 
**/

using System;
using System.Collections;

namespace Memcached.ClientLibrary
{
	/// <summary>
	/// Gives us a handy way to modify a collection while we're iterating through it.
	/// </summary>
	public class IteratorIsolateCollection: IEnumerable
	{
        IEnumerable _enumerable;

		public IteratorIsolateCollection(IEnumerable enumerable)
		{
			_enumerable = enumerable;
		}

		public IEnumerator GetEnumerator()
		{
			return new IteratorIsolateEnumerator(_enumerable.GetEnumerator());
		}

        internal class IteratorIsolateEnumerator : IEnumerator
        {
            ArrayList items = new ArrayList();
            int currentItem;

            internal IteratorIsolateEnumerator(IEnumerator enumerator)
            {
                while (enumerator.MoveNext() != false)
                {
                    items.Add(enumerator.Current);
                }
                IDisposable disposable = enumerator as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
                currentItem = -1;
            }

            public void Reset()
            {
                currentItem = -1;
            }

            public bool MoveNext()
            {
                currentItem++;
                if (currentItem == items.Count)
                    return false;

                return true;
            }

            public object Current
            {
                get
                {
                    return items[currentItem];
                }
            }
        }
	}
}
