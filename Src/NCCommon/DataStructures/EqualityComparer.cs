using System.Collections;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class EqualityComparer : IEqualityComparer
    {
        public new bool Equals(object x, object y)
        {
            return x == null && y == null ? true : x != null ? x.Equals(y) : false;
        }

        public int GetHashCode(object obj)
        {
            var stringObj = obj as string;

            return stringObj != null ? AppUtil.GetHashCode(stringObj) : obj.GetHashCode();
        }
    }
}
