using System.Diagnostics;
using System.Runtime;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    [DebuggerDisplay("{value}", Name = "[{key}]", Type = "")]
    public class KeyValuePairs
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object key;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object value;
        public object Key
        {
#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
#endif
            get
            {
                return this.key;
            }
        }
        public object Value
        {
#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
#endif
            get
            {
                return this.value;
            }
        }
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
#endif
        public KeyValuePairs(object key, object value)
        {
            this.value = value;
            this.key = key;
        }
    }
}