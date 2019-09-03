using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    internal sealed class VectorDebugView
    {
        private IDictionary dictionary;

        internal class DebugBucket
        {
            private object _key;
            private object _value;

            public DebugBucket(object key, object value)
            {
                _key = key;
                _value = value;
            }

            public object Key
            {
                get { return _key; }
            }

            public object Value
            {
                get { return _value; }
            }

            public override string ToString()
            {
                return _key.ToString() + " : " + _value.ToString();
            }
        }

        public VectorDebugView(IDictionary vector)
        {
            dictionary = vector;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IEnumerable<DebugBucket> Values
        {
            get
            {
                foreach (object key in dictionary.Keys)
                {
                    yield return new DebugBucket(key, dictionary[key]);
                }
            }
        }
    }
}