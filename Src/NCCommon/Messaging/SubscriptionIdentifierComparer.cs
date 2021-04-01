using Alachisoft.NCache.Runtime.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common
{
    public class SubscriptionIdentifierCompararer : System.Collections.IEqualityComparer
    {
        bool System.Collections.IEqualityComparer.Equals(object x, object y)
        {
            var isEqual = true;

            isEqual = isEqual && (x.GetType() == typeof(SubscriptionIdentifier));
            isEqual = isEqual && (y.GetType() == typeof(SubscriptionIdentifier));

            if (isEqual)
            {
                var unboxedX = x as SubscriptionIdentifier;
                var unboxedY = y as SubscriptionIdentifier;

                isEqual = isEqual && (unboxedX.SubscriptionPolicy == unboxedY.SubscriptionPolicy);
                isEqual = isEqual && unboxedX.SubscriptionName.Equals(unboxedY.SubscriptionName);
            }
            return isEqual;
        }

        int System.Collections.IEqualityComparer.GetHashCode(object obj)
        {
            if (obj is SubscriptionIdentifier)
            {
                var unboxedObj = obj as SubscriptionIdentifier;
                var hashCodeCatalyst = unboxedObj.SubscriptionPolicy
                                        + unboxedObj.SubscriptionName;
                return hashCodeCatalyst.GetHashCode();
            }
            return obj.GetHashCode();
        }
    }
}
