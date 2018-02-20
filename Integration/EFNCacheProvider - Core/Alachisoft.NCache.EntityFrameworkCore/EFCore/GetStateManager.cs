// Description: Entity Framework Bulk Operations & Utilities (EF Bulk SaveChanges, Insert, Update, Delete, Merge | LINQ Query Cache, Deferred, Filter, IncludeFilter, IncludeOptimize | Audit)
// Website & Documentation: https://github.com/zzzprojects/Entity-Framework-Plus
// Forum & Issues: https://github.com/zzzprojects/EntityFramework-Plus/issues
// License: https://github.com/zzzprojects/EntityFramework-Plus/blob/master/LICENSE
// More projects: http://www.zzzprojects.com/
// Copyright © ZZZ Projects Inc. 2014 - 2016. All rights reserved.

#if FULL
#if EFCORE
using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace Alachisoft.NCache.EntityFrameworkCore
{
    internal static partial class InternalExtensions
    {
        internal static StateManager GetStateManager(this ChangeTracker changeTracker)
        {
            var _stateManagerField = changeTracker.GetType().GetField("_stateManager", BindingFlags.NonPublic | BindingFlags.Instance);
            var stateManagerBoxed = _stateManagerField.GetValue(changeTracker);
            // Should not happen
            if (stateManagerBoxed == null)
                return null;
            else
                return (StateManager)stateManagerBoxed;
        }
    }
}
#endif
#endif