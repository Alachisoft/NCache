// Copyright (c) 2018 Alachisoft
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

using Alachisoft.NCache.EntityFrameworkCore.NCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Alachisoft.NCache.EntityFrameworkCore
{
    public static partial class DbContextExtensions
    {
        /// <summary>
        /// Returns the instance of the cache that is binded with the database context on which the method is called.
        /// </summary>
        /// <param name="context">The database context on which the method is called.</param>
        /// <returns>Returns the instance of the <see cref="Cache"/> binded with the database context.</returns>
        public static Cache GetCache(this DbContext context)
        {
            // This is only called to use the context so that onConfiguring method is called.
            var model = context.Model;

            Cache cache = new Cache
            {
                CurrentContext = context
            };

            return cache;
        }

        /// <summary>
        ///     I am sorry - Developer
        ///     [UPDATE 27th Oct, 2017] : I am even more sorry - Another Developer
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        internal static T GetRefValue<T>(this StateManager stateManager, T entityCache) where T : class
        {
            Dictionary<object, InternalEntityEntry> _entityReferenceMap = (Dictionary<object, InternalEntityEntry>)stateManager.GetType().GetField("_entityReferenceMap", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(stateManager);

            TrackingHelper trackingHelper = new TrackingHelper(stateManager.Context);

            trackingHelper.EnqueueEntity(entityCache);

            object entity = default(object);

            while ((entity = trackingHelper.DequeueEntity()) != null)
            {
                Func<object, bool> func = delegate (object entityStateManager)
                {
                    Type t1 = entityStateManager.GetType();
                    Type t2 = entity.GetType();
                    string methodName = nameof(trackingHelper.EntityComparer);

                    MethodInfo genericMethod = typeof(TrackingHelper).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(t1, t2);
                    return (bool)genericMethod.Invoke(trackingHelper, new object[] { entityStateManager, entity });
                };

                var mapVal = _entityReferenceMap.Keys.Where(func).FirstOrDefault();

                if (mapVal == null)
                {
                    /*
                     * So we have a case,
                     * 
                     *      01. An entity 'X' is queried from cache and hence tracked via EF's own tracking method (DbContext.Attach).
                     *      
                     *      02. Another entity 'Y', that has a relation with 'X', is queried with 'Include' operation in the query.
                     *      
                     *      03. The code before this method only compares 'Y' with entities already present in the state manager and 
                     *          not the entities inside 'Y' (inside because of either 1-1 or 1-* relation).
                     *      
                     *      04. As a result, when 'Y' is not found, an attempt to tracking 'Y' is made.
                     *      
                     *      05. Now EF will track 'Y' AND all the 'X's contained in 'Y'.
                     *      
                     *      06. Unfortunately, one of the 'X' is already being tracked and attempting to track 'Y' will attempt to 
                     *          track that 'X' too.
                     *      
                     *      07. Since these entities are the same but their references are different, the method used for tracking 
                     *          entities (from EF) will throw "entity already being tracked" exception.
                     *      
                     *      08. We need to replace those entities with the ones already being tracked so that they both have the same 
                     *          reference.
                     *      
                     *      09. Doing so will not invoke the exception.
                     */
                    trackingHelper.SafeAttach(_entityReferenceMap, entity);
                }
                else if (mapVal.GetType() == entityCache.GetType())
                {
                    /* We merged entity cache's data into the (updated) entity in state manager.
                     * Return value should be this updated entity for 2 reasons,
                     *
                     *      01. Entity in state manager may have something in it that entity in cache 
                     *          probably didn't have. Merging may have yielded a new entity with more 
                     *          data.
                     *          
                     *      02. Attaching entity from cache will throw attachement exceptions if user 
                     *          attached an entity on his/her own explicitly as entity from cache will 
                     *          still be different from entity in state manager if it wasn't replaced.
                     *          (This is the most important reason in my opinion)
                     */
                    entityCache = (T)mapVal;
                }
            }
            return entityCache;
        }
    }
}
