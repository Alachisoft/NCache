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

using System;
using System.Globalization;
using System.Collections;
using Alachisoft.NCache.Common.Util;
using System.Reflection;
using Alachisoft.NCache.Runtime.Caching;

namespace Alachisoft.NCache.Caching.CacheLoader
{
    internal class CacheLoaderUtil
    {
        internal static int EvaluateExpirationParameters(DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            if (DateTime.MaxValue.ToUniversalTime().Equals(absoluteExpiration) &&
                TimeSpan.Zero.Equals(slidingExpiration))
            {
                return 2;
            }

            if (DateTime.MaxValue.ToUniversalTime().Equals(absoluteExpiration))
            {
                if (slidingExpiration.CompareTo(TimeSpan.Zero) < 0)
                    throw new ArgumentOutOfRangeException("slidingExpiration");

                if (slidingExpiration.CompareTo(DateTime.Now.AddYears(1) - DateTime.Now) >= 0)
                    throw new ArgumentOutOfRangeException("slidingExpiration");

                return 0;
            }

            if (TimeSpan.Zero.Equals(slidingExpiration))
            {
                return 1;
            }

            throw new ArgumentException("You cannot set both sliding and absolute expirations on the same cache item.");
        }
        
        internal static void EvaluateTagsParameters(Hashtable queryInfo, string group)
        {

            if (queryInfo != null)
            {
                if (!String.IsNullOrEmpty(group) && queryInfo["tag-info"] != null)
                    throw new ArgumentException("You cannot set both groups and tags on the same cache item.");
            }

        }

        internal static Hashtable GetJavaTagInfo(string fullName, Tag[] tags)
        {
            if (tags == null) return null;

            Hashtable tagInfo = new Hashtable();
            ArrayList tagsList = new ArrayList();
            foreach (Tag tag in tags)
            {
                if (tag == null)
                {
                    throw new ArgumentNullException("Tag");
                }
                if (tag.TagName != null)
                {
                    tagsList.Add(tag.TagName);
                }
            }

            tagInfo["type"] = fullName;
            tagInfo["tags-list"] = tagsList;

            return tagInfo;
        }


        internal static Hashtable GetTagInfo(object value, Tag[] tags)
		{
            if (tags == null) return null;

            Hashtable tagInfo = new Hashtable();
            ArrayList tagsList = new ArrayList();
            foreach (Tag tag in tags)
            {
                if (tag == null)
				{
					throw new ArgumentNullException("Tag");
				}
                if (tag.TagName != null)
                {
                    tagsList.Add(tag.TagName);
                }
            }

            tagInfo["type"] = value.GetType().FullName;
            tagInfo["tags-list"] = tagsList;

            return tagInfo;
        }

        internal static Hashtable GetJavaNamedTagsInfo(string fullName, NamedTagsDictionary namedTags, TypeInfoMap typeMap)
        {
            CheckJavaDuplicateIndexName(fullName, namedTags, typeMap);

            if (namedTags == null || namedTags.Count == 0)
                return null;

            Hashtable tagInfo = new Hashtable();
            Hashtable tagsList = new Hashtable();

            foreach (DictionaryEntry nameValue in namedTags)
            {
                if (nameValue.Value == null)
                {
                    throw new ArgumentNullException("Named Tag value cannot be null");
                }

                tagsList.Add(nameValue.Key, nameValue.Value);
            }

            string typeName = fullName;
            typeName = typeName.Replace("+", ".");

            tagInfo["type"] = typeName;
            tagInfo["named-tags-list"] = tagsList;

            return tagInfo;
        }

        private static void CheckJavaDuplicateIndexName(string fullName, NamedTagsDictionary namedTags, TypeInfoMap typeMap)
        {
            if (namedTags == null || typeMap == null)
            {
                return;
            }

            string typeName = fullName;
            typeName = typeName.Replace("+", ".");

            int handleId = typeMap.GetHandleId(typeName);
            if (handleId != -1)
            {
                ArrayList attributes = typeMap.GetAttribList(handleId);
                foreach (string name in attributes)
                {
                    if (namedTags.Contains(name)) 
                    {
                        throw new Exception("Key in named tags conflicts with the indexed attribute name of the specified object.");
                    }
                }
            }
        }


        //-

        internal static Hashtable GetNamedTagsInfo(object value, NamedTagsDictionary namedTags, TypeInfoMap typeMap)
        {
            CheckDuplicateIndexName(value, namedTags, typeMap);

            if (namedTags == null || namedTags.Count == 0)
                return null;

            Hashtable tagInfo = new Hashtable();
            Hashtable tagsList = new Hashtable();

            foreach (DictionaryEntry nameValue in namedTags)
            {
                if (nameValue.Key != null && string.IsNullOrEmpty(nameValue.Key.ToString().Trim()))
                    throw new ArgumentException("Named Tag key cannot be null or empty");

                if (nameValue.Value == null)
                {
                    throw new ArgumentNullException("Named Tag value cannot be null");
                }

                tagsList.Add(nameValue.Key, nameValue.Value);
            }

            string typeName = value.GetType().FullName;
            typeName = typeName.Replace("+", ".");

            tagInfo["type"] = typeName;
            tagInfo["named-tags-list"] = tagsList;

            return tagInfo;
        }

        private static void CheckDuplicateIndexName(object value, NamedTagsDictionary namedTags, TypeInfoMap typeMap)
        {
            if (namedTags == null || value == null || typeMap == null)
            {
                return;
            }

            string typeName = value.GetType().FullName;
            typeName = typeName.Replace("+", ".");

            int handleId = typeMap.GetHandleId(typeName);
            if (handleId != -1)
            {
                ArrayList attributes = typeMap.GetAttribList(handleId);
                foreach (string name in attributes)
                {
                    if (namedTags.Contains(name)) //@UH whether this should be case insensitive
                    {
                        throw new Exception("Key in named tags conflicts with the indexed attribute name of the specified object.");
                    }
                }
            }
        }

        internal static Hashtable GetQueryInfo(Object value, TypeInfoMap typeMap)
        {
            Hashtable queryInfo = null;

            if (typeMap == null)
                return null;

            try
            {
                int handleId = typeMap.GetHandleId(value.GetType().FullName);
                if (handleId != -1)
                {
                    queryInfo = new Hashtable();
                    ArrayList attribValues = new ArrayList();
                    ArrayList attributes = typeMap.GetAttribList(handleId);
                    for (int i = 0; i < attributes.Count; i++)
                    {
                        PropertyInfo propertyAttrib = value.GetType().GetProperty((string)attributes[i]);
                        if (propertyAttrib != null)
                        {
                            Object attribValue = propertyAttrib.GetValue(value, null);

                            if (attribValue is String) //add all strings as lower case in index tree
                            {
                                attribValue = attribValue.ToString();//.ToLower();
                            }

                            if (attribValue is DateTime) //add all DateTime as ticks
                            {
                                attribValue = ((DateTime)(attribValue)).Ticks.ToString(CultureInfo.InvariantCulture);
                            }

                            attribValues.Add(attribValue);
                        }
                        else
                        {
                            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance;

                            FieldInfo fieldAttrib = value.GetType().GetField((string)attributes[i], flags);
                            if (fieldAttrib != null)
                            {
                                Object attribValue = fieldAttrib.GetValue(value);

                                if (attribValue is String) //add all strings as lower case in index tree
                                {
                                    attribValue = attribValue.ToString();
                                }

                                if (attribValue is DateTime) //add all DateTime as ticks
                                {
                                    attribValue = ((DateTime)(attribValue)).Ticks.ToString(CultureInfo.InvariantCulture);
                                }

                                attribValues.Add(attribValue);
                            }
                            else
                            {
                                throw new Exception("Unable extracting query information from user object.");
                            }
                        }
                    }
                    queryInfo.Add(handleId, attribValues);
                }
            }
            catch (Exception) { }
            return queryInfo;
        }
    }
}
