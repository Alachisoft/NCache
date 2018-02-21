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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Alachisoft.NCache.EntityFrameworkCore.NCache
{
    class TrackingHelper
    {
        /*
         * NOTE : This code is an abyss (at least for me) and I wrote it.
         *        There are a lot of stuff that I aim to improve here.
         */

        private static object queueSyncLock = new object();

        private MethodInfo methodHashSetUnion;
        private MethodInfo methodGetRelationSetValue;
        private MethodInfo methodCopyRelationElements;
        private MethodInfo methodSetThingsUpForAttachment;

        private Queue nestedObjs;
        private object peekMemory;      // Saving this for something in my mind
        private DbContext currentContext;

        internal TrackingHelper(DbContext context)
        {
            methodHashSetUnion = typeof(TrackingHelper).GetMethod(
                nameof(HashSetUnion), BindingFlags.Instance | BindingFlags.NonPublic
            );
            methodGetRelationSetValue = typeof(TrackingHelper).GetMethod(
                nameof(GetRelationSetValue), BindingFlags.Instance | BindingFlags.NonPublic
            );
            methodCopyRelationElements = typeof(TrackingHelper).GetMethod(
                nameof(CopyRelationElements), BindingFlags.Instance | BindingFlags.NonPublic
            );
            methodSetThingsUpForAttachment = typeof(TrackingHelper).GetMethod(
                nameof(SetThingsUpForAttachment), BindingFlags.Instance | BindingFlags.NonPublic
            );
            nestedObjs = new Queue();
            currentContext = context;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------

        internal bool EntityComparer<T1, T2>(T1 entityStateManager, T2 entityCache)
        {
            /*  -----------------------------------------------------------------------
             *  NOTE: Returning 'false' means the two entities are not equal (or same).
             *  -----------------------------------------------------------------------
             */
            if (entityStateManager == null || entityCache == null)
            {
                // If one of them or both are null, they are not equal.
                // (They're same if they're both null though but still)
                return false;
            }
            if (entityStateManager.GetType() != entityCache.GetType())
            {
                // If their types do not match, they're not the same entites.
                return false;
            }
            if (Convert.GetTypeCode(entityStateManager) != TypeCode.Object)
            {
                // If entity is not an object, it means it's not an entity but 
                // a value of some property. Return the comparison of the two then.
                return entityStateManager.Equals(entityCache);
            }
            if (!DbContextUtils.IsEntity(currentContext, entityStateManager) || !DbContextUtils.IsEntity(currentContext, entityCache))
            {
                // If any or both the objects passed are not entities, don't go further.
                return false;
            }
            if (AreTheseEntitiesTheSame(entityStateManager, entityCache))
            {
                // Handle relations here
                UpdateOneToManyRelations(entityStateManager, entityCache);
                UpdateOneToOneRelations(entityStateManager, entityCache);
                return true;
            }
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------

        internal void SafeAttach(Dictionary<object, InternalEntityEntry> entityReferenceMap, object entityToBeAttached)
        {
            /* Entity passed here can be assumed to be entity from state manager (ESM).
             * 
             * Here's what needs to be done here,
             * 
             *      01. Extract relations from ESM.
             *      
             *      02. Replace entities in each relation from ESM with ones from state manager
             *          provided that exist in state manager and relation in ESM has been requested.
             *      
             *      03. This can be checked by first checking if relation has been requested or not.
             *      
             *      04. If relation is requested, the instance's value in ESM won't be null or empty 
             *          enumerable in case of 1-* relationship.
             *      
             *      05. Traverse entites from state manager and compare them with entities from ESM's
             *          relations.
             *      
             *      06. If match is found, remove it from ESM and add from state manager into ESM.
             *      
             *      07. Also update reference for that entity to ESM.
             */

            // ------------------------------------------------------------------------------------------------------- //
            //                                          -- AND SO IT BEGINS --                                         //
            // ------------------------------------------------------------------------------------------------------- //

            HashSet<object> entityTrack = new HashSet<object>();

            // Everything was moved here ↓
            SafeAttachInternal(entityReferenceMap, entityToBeAttached, entityTrack);

            // Attach entity to be tracked now
            currentContext.Attach(entityToBeAttached);
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------

        #region --                                                      Tracking Helpers                                                                   --

        private void UpdateOneToOneRelations<T1, T2>(T1 entityStateManager, T2 entityCache)
        {
            // Get all members that correspond to one-to-one relationships.
            foreach (MemberInfo member in GetRelationMembers(entityStateManager))
            {
                // If the entity in state manager does not have its value set to something else,
                if (GetMemberValue(member, entityStateManager) == null)
                {
                    object value = GetMemberValue(member, entityCache);
                    // Update its value with the value from entity from cache.
                    SetMemberValue(member, entityStateManager, value);

                    // Add resulting value into nested objs because we will be traversing them after this.
                    EnqueueEntity(value);
                }
            }
        }

        private void UpdateOneToManyRelations<T1, T2>(T1 entityStateManager, T2 entityCache)
        {
            foreach (MemberInfo hashSetMember in GetRelationSets(entityStateManager))
            {
                /*
                 * Make a generic method for 'GetRelationSetValue' and get the HashSets that correspond to table relations.
                 */
                Type genericTypeForGetRelationSetValueT1 = entityStateManager.GetType();
                Type genericTypeForGetRelationSetValueT2 = GetMemberType(hashSetMember).GenericTypeArguments[0];
                MethodInfo genericGetRelationSetValue = methodGetRelationSetValue.MakeGenericMethod(genericTypeForGetRelationSetValueT1, genericTypeForGetRelationSetValueT2);

                var hsStateManager = genericGetRelationSetValue.Invoke(this, new object[] { hashSetMember, entityStateManager });
                var hsCache = genericGetRelationSetValue.Invoke(this, new object[] { GetRelationSet(entityCache, hashSetMember), entityCache });

                if (hsStateManager == null || hsCache == null)
                {
                    continue;
                }

                /*
                 * Make a generic method for 'HashSetUnion' and populate a new HashSet with unique values.
                 */
                Type genericTypeForHashSetUnionT1 = hsStateManager.GetType().GetGenericArguments()[0];
                Type genericTypeForHashSetUnionT2 = hsCache.GetType().GetGenericArguments()[0];
                MethodInfo genericHashSetUnion = methodHashSetUnion.MakeGenericMethod(genericTypeForHashSetUnionT1, genericTypeForHashSetUnionT2);

                var result = genericHashSetUnion.Invoke(this, new object[] { hsStateManager, hsCache });

                if (result == null)
                {
                    continue;
                }

                /*
                 * Update the HashSet for entity in state manager.
                 */
                Type genericTypeForCopyRelationElementsT1 = entityStateManager.GetType();
                Type genericTypeForCopyRelationElementsT2 = result.GetType().GetGenericArguments()[0];
                MethodInfo genericCopyRelationElements = methodCopyRelationElements.MakeGenericMethod(genericTypeForCopyRelationElementsT1, genericTypeForCopyRelationElementsT2);

                genericCopyRelationElements.Invoke(this, new object[] { hashSetMember, entityStateManager, result });

                // Cast and add resulting hashset into nested objs because we will be traversing them after this.
                if ((result as dynamic).Count > 0)
                {
                    foreach (var elem in (result as IEnumerable))
                    {
                        EnqueueEntity(elem);
                    }
                }
            }
        }

        #endregion

        #region --                                                      Attaching Helpers                                                                  --

        private void SafeAttachInternal(Dictionary<object, InternalEntityEntry> entityReferenceMap, object entityAtHand, HashSet<object> entityTrack)
        {
            if (entityReferenceMap == null || entityAtHand == null)
            {
                return;
            }

            // Handling 1-1 relationships
            foreach (MemberInfo member in GetRelationMembers(entityAtHand))
            {
                var relationMemberValue = GetMemberValue(member, entityAtHand);

                if (relationMemberValue != null)
                {
                    Type genericTypeSetThingsUpForAttachmentTFromMember = GetMemberType(member);

                    Action<object> action = new Action<object>(delegate (object entityStateManager)
                    {
                        Type genericTypeSetThingsUpForAttachmentTFromStateManager = entityStateManager.GetType();

                        if (genericTypeSetThingsUpForAttachmentTFromStateManager == genericTypeSetThingsUpForAttachmentTFromMember)
                        {
                            // Make generic method
                            MethodInfo genericSetThingsUpForAttachment = methodSetThingsUpForAttachment.MakeGenericMethod(
                                genericTypeSetThingsUpForAttachmentTFromStateManager
                            );

                            // Instantiate generic hashset and add 'relationMemberValue' to it
                            Type genericType = typeof(HashSet<>).MakeGenericType(genericTypeSetThingsUpForAttachmentTFromStateManager);
                            var hashSet = Activator.CreateInstance(genericType);
                            genericType.GetMethod("Add").Invoke(hashSet, new object[] { relationMemberValue });

                            // Invoke 'SetThingsUpForAttachment' now
                            genericSetThingsUpForAttachment.Invoke(this, new object[] { entityReferenceMap, entityStateManager, hashSet, entityTrack });
                        }
                        else
                        {
                            if (!entityTrack.Contains(relationMemberValue))
                            {
                                entityTrack.Add(relationMemberValue);
                                SafeAttachInternal(entityReferenceMap, relationMemberValue, entityTrack);
                            }
                        }
                    });
                    // Run the above action for all entities in state manager
                    entityReferenceMap.Keys.ToList().ForEach(action);
                }
            }

            // Handling 1-* relationships
            foreach (MemberInfo member in GetRelationSets(entityAtHand))
            {
                var relationSetValue = GetMemberValue(member, entityAtHand);

                if (relationSetValue != null)
                {
                    Type genericTypeSetThingsUpForAttachmentTFromMember = GetMemberType(member).GenericTypeArguments[0];

                    Action<object> action = new Action<object>(delegate (object entityStateManager)
                    {
                        Type genericTypeSetThingsUpForAttachmentTFromStateManager = entityStateManager.GetType();

                        if (genericTypeSetThingsUpForAttachmentTFromStateManager == genericTypeSetThingsUpForAttachmentTFromMember)
                        {
                            MethodInfo genericSetThingsUpForAttachment = methodSetThingsUpForAttachment.MakeGenericMethod(
                                genericTypeSetThingsUpForAttachmentTFromStateManager
                            );
                            genericSetThingsUpForAttachment.Invoke(this, new object[] { entityReferenceMap, entityStateManager, relationSetValue, entityTrack });
                        }
                        else
                        {

                        }
                    });
                    // Run the above action for all entities in state manager
                    entityReferenceMap.Keys.ToList().ForEach(action);
                }
            }
        }

        private void SetThingsUpForAttachment<T>(Dictionary<object, InternalEntityEntry> entityReferenceMap, T entityStateManager, HashSet<T> hsEntitiesFromTracked, HashSet<object> entityTrack)
        {
            if (entityStateManager == null || hsEntitiesFromTracked == null)
            {
                return;
            }

            // Populate with items to hold and add all later to the needed entity
            IList<T> entitiesToMergeLater = new List<T>();

            foreach (T entityFromTracked in hsEntitiesFromTracked.ToList())
            {
                if (!entityTrack.Contains(entityFromTracked))
                {
                    entityTrack.Add(entityFromTracked);
                    SafeAttachInternal(entityReferenceMap, entityFromTracked, entityTrack);
                }
                if (AreTheseEntitiesTheSame(entityStateManager, entityFromTracked))
                {
                    // Handle relations here
                    UpdateOneToOneRelationsForAttaching(entityStateManager, entityFromTracked);
                    UpdateOneToManyRelationsForAttaching(entityStateManager, entityFromTracked);

                    entitiesToMergeLater.Add(entityStateManager);
                }
                else
                {
                    entitiesToMergeLater.Add(entityFromTracked);
                }
            }
            hsEntitiesFromTracked.Clear();

            foreach (T entityThatShouldBeMergedNow in entitiesToMergeLater)
            {
                hsEntitiesFromTracked.Add(entityThatShouldBeMergedNow);
            }
        }

        private void UpdateOneToOneRelationsForAttaching<T1, T2>(T1 entityStateManager, T2 entityTracked)
        {
            // Get all members that correspond to one-to-one relationships.
            foreach (MemberInfo member in GetRelationMembers(entityStateManager))
            {
                // If the entity in state manager does not have its value set to something else,
                if (GetMemberValue(member, entityStateManager) == null)
                {
                    object value = GetMemberValue(member, entityTracked);
                    // Update its value with the value from entity to be tracked. 
                    // Whether the value from entity to be tracked is null or not 
                    // has no effect on the ongoing process.
                    SetMemberValue(member, entityStateManager, value);
                }
                else
                {
                    // Else if entity from state manager has a value set,
                    object value = GetMemberValue(member, entityTracked);
                    // Update that value in entity to be tracked as we 
                    // are preferncing entity from state manager and 
                    // attaching entity to be tracked after this will 
                    // not throw any attachement exceptions.
                    SetMemberValue(member, entityTracked, value);
                }
            }
        }

        private void UpdateOneToManyRelationsForAttaching<T1, T2>(T1 entityStateManager, T2 entityFromTracked)
        {
            foreach (MemberInfo hashSetMember in GetRelationSets(entityStateManager))
            {
                /*
                 * Make a generic method for 'GetRelationSetValue' and get the HashSets that correspond to table relations.
                 */
                Type genericTypeForGetRelationSetValueT1 = entityStateManager.GetType();
                Type genericTypeForGetRelationSetValueT2 = GetMemberType(hashSetMember).GenericTypeArguments[0];
                MethodInfo genericGetRelationSetValue = methodGetRelationSetValue.MakeGenericMethod(genericTypeForGetRelationSetValueT1, genericTypeForGetRelationSetValueT2);

                var hsStateManager = genericGetRelationSetValue.Invoke(this, new object[] { hashSetMember, entityStateManager });
                var hsTracked = genericGetRelationSetValue.Invoke(this, new object[] { GetRelationSet(entityFromTracked, hashSetMember), entityFromTracked });

                if (hsStateManager == null || hsTracked == null)
                {
                    continue;
                }

                /*
                 * Make a generic method for 'HashSetUnion' and populate a new HashSet with unique values.
                 */
                Type genericTypeForHashSetUnionT1 = hsStateManager.GetType().GetGenericArguments()[0];
                Type genericTypeForHashSetUnionT2 = hsTracked.GetType().GetGenericArguments()[0];
                MethodInfo genericHashSetUnion = methodHashSetUnion.MakeGenericMethod(genericTypeForHashSetUnionT1, genericTypeForHashSetUnionT2);

                var result = genericHashSetUnion.Invoke(this, new object[] { hsStateManager, hsTracked });

                if (result == null)
                {
                    continue;
                }

                /*
                 * Update the HashSet for entity in state manager.
                 */
                Type genericTypeForCopyRelationElementsT1 = entityStateManager.GetType();
                Type genericTypeForCopyRelationElementsT2 = result.GetType().GetGenericArguments()[0];
                MethodInfo genericCopyRelationElements = methodCopyRelationElements.MakeGenericMethod(genericTypeForCopyRelationElementsT1, genericTypeForCopyRelationElementsT2);

                genericCopyRelationElements.Invoke(this, new object[] { hashSetMember, entityStateManager, result });

                // Also update for entity to be tracked as we are preferncing entity from state manager and 
                // attaching entity to be tracked after this will not throw any attachement exceptions.
                genericCopyRelationElements.Invoke(this, new object[] { hashSetMember, entityFromTracked, result });
            }
        }

        #endregion

        #region --                                                      Generic Helpers                                                                    --

        private bool AreTheseEntitiesTheSame<T1, T2>(T1 entityStateManager, T2 entityCache)
        {
            // If any or both of the entities passed are null,
            if (entityStateManager == null || entityCache == null)
            {
                // Return false to indicate they are not equal.
                return false;
            }

            // Get the values of primary keys of both the entities.
            object pkValEntStateManager = DbContextUtils.GetPrimaryKeyValue(currentContext, entityStateManager);
            object pkValEntCache = DbContextUtils.GetPrimaryKeyValue(currentContext, entityCache);

            // If successfully got the values for primary keys of both the entities,
            if (pkValEntStateManager != null && pkValEntCache != null)
            {
                // Return their value comparison.
                return pkValEntStateManager.Equals(pkValEntCache);
            }
            /*
             * Otherwise, compare both entities via reflection.
             */
            if (!FieldCompare(entityStateManager, entityCache))
            {
                /*
                 * Satisfaction of this if-condition could mean any of
                 * the following,
                 * 
                 *      01. 'entityCache's values for fields were not 
                 *          the same as that of 'entityStateManager'.
                 *          
                 *      02. 'entityCache' was not the same as 
                 *          'entityStateManager'.
                 */
                return false;
            }
            if (!PropertyCompare(entityStateManager, entityCache))
            {
                /*
                 * Satisfaction of this if-condition could mean any of
                 * the following,
                 * 
                 *      01. 'entityCache's values for properties were not 
                 *          the same as that of 'entityStateManager'.
                 *          
                 *      02. 'entityCache' was not the same as 
                 *          'entityStateManager'.
                 */
                return false;
            }

            // Reaching here means both the entities are the same.
            return true;
        }

        private HashSet<T1> HashSetUnion<T1, T2>(HashSet<T1> first, HashSet<T2> second)
        {
            /*
             * NOTE,
             *      first   :   Hashset from entity from State Manager
             *      second  :   Hashset from entity from Cache
             */
            // Create an empty hashset.
            HashSet<T1> union = new HashSet<T1>();

            // Add all elements from 'first' because they're more ~updated~.
            if (first != null)
            {
                if (first.Count > 0)
                {
                    union.UnionWith(first);
                }
            }
            else
            {
                first = new HashSet<T1>();
            }
            if (second != null)
            {
                if (second.Count > 0)
                {
                    // Remove the elements from 'second' that exist in 'first'.
                    Predicate<T2> predicate = delegate (T2 relElemSec)
                    {
                        return first.Any(relElemStateManager => AreTheseEntitiesTheSame(relElemStateManager, relElemSec));
                    };
                    second.RemoveWhere(predicate);

                    // If elements exist in 'second',
                    if (second.Count > 0)
                    {
                        // Add them into 'union' because they are missing from 'first'.
                        union.UnionWith(second.Cast<T1>());
                    }

                    first.Clear();
                    second.Clear();
                    first.UnionWith(union);
                    second.UnionWith(union.Cast<T2>());
                }
            }
            return union;
        }

        private void UpdateOneToManyRelationReferences<T1, T2>(T1 entity, HashSet<T2> hashSet)
        {
            /*
             * Can't update entity if it's null and can't update entity with hashset if 
             * the hashset is null.
             */
            if (hashSet != null && entity != null)
            {
                // Can't update entity with hashset if hashset is empty.
                if (hashSet.Count > 0)
                {
                    Type type = hashSet.First().GetType();
                    MemberInfo memberInterestedIn = default(MemberInfo);

                    /*
                     * By interested member, we mean the reference of parent table in child elements.
                     */
                    foreach (MemberInfo member in type.GetMembers())
                    {
                        if (GetMemberType(member) == entity.GetType())
                        {
                            memberInterestedIn = member;
                            break;
                        }
                    }
                    // If interested member exists.
                    if (memberInterestedIn != default(MemberInfo))
                    {
                        // Iterate on all entities in question and update their reference to parent's reference.
                        hashSet.AsParallel().ForAll(
                            delegate (T2 elem)
                            {
                                SetMemberValue(memberInterestedIn, elem, entity);
                            }
                        );
                    }
                }
            }
        }

        private void CopyRelationElements<T1, T2>(MemberInfo memberInfo, T1 entity, HashSet<T2> relationElements)
        {
            // Get hashset from entity in state manager.
            HashSet<T2> relRef = GetRelationSetValue<T1, T2>(memberInfo, entity);
            if (relRef != relationElements)
            {
                // Empty it.
                relRef.Clear();
                // Add elements from 'relationElements'. They contain the elements already in 
                // hashset from entity in state manager beforehand too.
                relRef.UnionWith(relationElements);

                // Update references of children to parent.
                UpdateOneToManyRelationReferences(entity, relRef);
                // Update entity in state manager with the update hashset (just in case).
                SetRelationSetValue(memberInfo, entity, relationElements);
            }
        }

        #endregion

        #region --                                                Reflection (Tracking) Helpers                                                            --

        private bool FieldCompare<T1, T2>(T1 first, T2 second)
        {
            // If any or both of the objects passed are null,
            if (first == null || second == null)
            {
                // Return false to indicate they are not equal.
                return false;
            }

            Type typeFirst = first.GetType();
            Type typeSecond = second.GetType();
            BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            /*
             * Confirming first's fields with second's only.
             */
            foreach (FieldInfo fieldFirst in typeFirst.GetFields())
            {
                FieldInfo fieldSecond = typeSecond.GetField(fieldFirst.Name, bindingFlags);

                if (fieldSecond == null)
                {
                    // Satisfaction of this if-condition means that 'entityCache' did not contain 
                    // the field under study.
                    return false;
                }
                if (DbContextUtils.IsEntity(currentContext, fieldFirst.DeclaringType) || fieldFirst.DeclaringType == typeof(HashSet<>))
                {
                    // Ignore checking if the field under question is an entity or a hashset (one-to-many relationship).
                    continue;
                }
                if (!fieldFirst.GetValue(first).Equals(fieldSecond.GetValue(second)))
                {
                    // Return false upon first mismatch of any field.
                    return false;
                }
            }
            return true;
        }

        private bool PropertyCompare<T1, T2>(T1 first, T2 second)
        {
            // If any or both of the objects passed are null,
            if (first == null || second == null)
            {
                // Return false to indicate they are not equal.
                return false;
            }

            Type typeFirst = first.GetType();
            Type typeSecond = second.GetType();
            BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            /*
             * Confirming first's properties with second's only.
             */
            foreach (PropertyInfo propertyFirst in typeFirst.GetProperties())
            {
                PropertyInfo propertySecond = typeSecond.GetProperty(propertyFirst.Name, bindingFlags);

                if (propertySecond == null)
                {
                    // Satisfaction of this if-condition means that 'entityCache' did not contain 
                    // the field under study.
                    return false;
                }
                if (DbContextUtils.IsEntity(currentContext, propertyFirst.DeclaringType) || propertyFirst.DeclaringType == typeof(HashSet<>))
                {
                    // Ignore checking if the property under question is an entity or a hashset (one-to-many relationship).
                    continue;
                }
                if (!propertyFirst.GetValue(first).Equals(propertySecond.GetValue(second)))
                {
                    // Return false upon first mismatch of any property.
                    return false;
                }
            }
            return true;
        }

        private MemberInfo[] GetRelationSets<T>(T entity)
        {
            if (entity == null)
            {
                // No need to go further if the entity passed is null.
                return new MemberInfo[] { };
            }

            // Get all fields and properties of entity passed.
            Type entityType = entity.GetType();
            IList<MemberInfo> hashSetMembers = new List<MemberInfo>();
            MemberInfo[] members = entityType.GetFields().Cast<MemberInfo>().Concat(entityType.GetProperties()).ToArray();

            // Populate list with members that correspond to relationship between entities.
            foreach (MemberInfo member in members)
            {
                Type type = GetMemberType(member);

                if (type.IsGenericType && type.IsInterface /*&& type.GetGenericTypeDefinition() == typeof(HashSet<>)*/)
                {
                    // If it's a generic type, an interface and a hashset, it's a relationship member.
                    hashSetMembers.Add(member);
                }
            }
            return hashSetMembers.ToArray();
        }

        private MemberInfo[] GetRelationMembers<T>(T entity)
        {
            if (entity == null)
            {
                // No need to go further if the entity passed is null.
                return new MemberInfo[] { };
            }

            // Get all fields and properties of entity passed.
            Type entityType = entity.GetType();
            IList<MemberInfo> relationMembers = new List<MemberInfo>();
            MemberInfo[] members = entityType.GetFields().Cast<MemberInfo>().Concat(entityType.GetProperties()).ToArray();

            // Populate list with members that correspond to relationship between entities.
            foreach (MemberInfo member in members)
            {
                Type type = GetMemberType(member);

                if (DbContextUtils.IsEntity(currentContext, type))
                {
                    // If it's an entity type, it's a relationship member.
                    relationMembers.Add(member);
                }
            }
            return relationMembers.ToArray();
        }

        private MemberInfo GetRelationSet<T>(T entity, MemberInfo memberInfo)
        {
            return (entity == null || memberInfo == null) ? null : GetRelationSet(entity, memberInfo.Name);
        }

        private MemberInfo GetRelationSet<T>(T entity, string memberName)
        {
            if (entity == null || memberName == null || string.IsNullOrEmpty(memberName.Trim()))
            {
                return null;
            }
            return entity.GetType().GetMember(memberName).FirstOrDefault();
        }

        private void SetRelationSetValue<T1, T2>(MemberInfo member, T1 instance, HashSet<T2> hashSet)
        {
            if (member == null || instance == null || hashSet == null)
            {
                return;
            }
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    (member as FieldInfo).SetValue(instance, hashSet);
                    break;
                case MemberTypes.Property:
                    (member as PropertyInfo).SetValue(instance, hashSet);
                    break;
                default:
                    break;
            }
        }

        private HashSet<TReturn> GetRelationSetValue<TSource, TReturn>(MemberInfo member, TSource instance)
        {
            if (member == null || instance == null)
            {
                return default(HashSet<TReturn>);
            }
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return (HashSet<TReturn>)(member as FieldInfo).GetValue(instance);
                case MemberTypes.Property:
                    return (HashSet<TReturn>)(member as PropertyInfo).GetValue(instance);
                default:
                    return default(HashSet<TReturn>);
            }
        }

        #endregion

        #region --                                                Reflection (Simple) Helpers                                                              --

        private Type GetMemberType(MemberInfo member)
        {
            if (member == null)
            {
                return default(Type);
            }
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return (member as FieldInfo).FieldType;
                case MemberTypes.Property:
                    return (member as PropertyInfo).PropertyType;
                default:
                    return default(Type);
            }
        }

        private void SetMemberValue(MemberInfo memberInfo, object instance, object value)
        {
            if (memberInfo != null && instance != null && value != null)
            {
                switch (memberInfo.MemberType)
                {
                    case MemberTypes.Field:
                        (memberInfo as FieldInfo).SetValue(instance, value);
                        break;
                    case MemberTypes.Property:
                        (memberInfo as PropertyInfo).SetValue(instance, value);
                        break;
                    default:
                        break;
                }
            }
        }

        private object GetMemberValue(MemberInfo member, object instance)
        {
            if (member != null && instance != null)
            {
                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        return (member as FieldInfo).GetValue(instance);
                    case MemberTypes.Property:
                        return (member as PropertyInfo).GetValue(instance);
                }
            }
            return default(object);
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------------------------------------------

        #region --                                                  Entity Queue Operations                                                                --

        internal void EnqueueEntity(object entity)
        {
            // Double check locking to strongly avoid entry of any object 
            // that is already added.
            if (nestedObjs != default(Queue))
            {
                if (!nestedObjs.Contains(entity))
                {
                    lock (queueSyncLock)
                    {
                        if (!nestedObjs.Contains(entity))
                        {
                            nestedObjs.Enqueue(entity);
                        }
                    }
                }
            }
        }

        internal object DequeueEntity()
        {
            // Double check locking to strongly avoid exception thrown 
            // when dequeueing an empty queue.
            if (nestedObjs != default(Queue))
            {
                if (nestedObjs.Count > 0)
                {
                    lock (queueSyncLock)
                    {
                        if (nestedObjs.Count > 0)
                        {
                            return nestedObjs.Dequeue();
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
