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
