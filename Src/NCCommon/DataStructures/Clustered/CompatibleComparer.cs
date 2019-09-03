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