// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using System.Collections;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Sample.Data;
using System.Configuration;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that demonstrates the usage of tags in NCache
    /// </summary>
    public class Tags
    {
        private static Cache _cache;

        public static void Run()
        {
            InitializeCache();

            // Adding items with named Tags       
            // These tags are used to identify products who are ISOCertified, QCPassed and fall in ClassA
            Tag[] tagList = new Tag[3];
            tagList[0] = new Tag("ISOCertified");
            tagList[1] = new Tag("QCPassed");
            tagList[2] = new Tag("ClassA");

            // Add Items in cache
            AddItems(tagList);

            // Get Items using tags
            GetItemsWithTag();


            // Get keys by tags
            // Here keys can be retrived from the cache via following three methods
            // Get keys by a specific tag
            GetKeysByTag(tagList[0]);

            // Get keys where all the tags match
            GetKeysByMultipleTags(tagList);

            // Get keys where any item in tagList matches
            GetKeysByAnyTag(tagList);


            // Get Data by tags
            // Here values can be retrived from the cache via following three methods
            // Get values by specified tag
            GetValuesByTag(tagList[0]);

            // Get those values only where complete tagList matches
            GetValuesByMultipleTags(tagList);

            // Retrivies values where any item in tagList matches
            GetValuesByAnyTag(tagList);

            // Remove items from Cache by tags
            // Removes values by specified tag
            RemoveItemsByTag(tagList[0]);

            // Removes those values only where complete tagList matches
            RemoveItemsByMultipleTags(tagList);

            // Removes values where any item in tagList matches
            RemoveItemsByAnyTag(tagList);

            Console.WriteLine();

            // Dispose cache once done
            _cache.Dispose();
        }

        /// <summary>
        /// This method initializes the cache.
        /// </summary>
        private static void InitializeCache()
        {
            string cache = ConfigurationManager.AppSettings["CacheId"];

            if (String.IsNullOrEmpty(cache))
            {
                Console.WriteLine("The Cache Name cannot be null or empty.");
                return;
            }

            // Initialize an instance of the cache to begin performing operations:
            _cache = NCache.Web.Caching.NCache.InitializeCache(cache);

            Console.WriteLine("Cache initialized successfully");
        }

        /// <summary>
        /// This method add items in the cache.
        /// </summary>
        /// <param name="tagList"> Tags that will be added with the items. </param>
        private static void AddItems(Tag[] tagList)
        {
            //4 Items are added to the cache
            _cache.Add("Product:MobilePhone1", new Product() { Id = 1, Name = "ProductID: XYZ", Category = "Z" }, tagList);
            _cache.Add("Product:MobilePhone2", new Product() { Id = 1, Name = "ProductID: ABC", Category = "Z" }, tagList);
            _cache.Add("Product:MobilePhone3", new Product() { Id = 1, Name = "ProductID: 123", Category = "Z" });
            _cache.Add("Product:MobilePhone4", new Product() { Id = 1, Name = "ProductID: 456", Category = "Z" });
        }

        /// <summary>
        /// This method fetches items from the cache.
        /// </summary>
        private static void GetItemsWithTag()
        {
            //Retrieve items who are QCPassed
            Tag itemTag = new Tag("QCPassed");

            Hashtable items = _cache.GetByTag(itemTag);
            if (items.Count > 0)
            {
                IEnumerator iter = items.Values.GetEnumerator();
                while (iter.MoveNext())
                {
                    Console.WriteLine(((Product)iter.Current).Name);
                }
            }
        }

        /// <summary>
        /// This method fetches keys from the cache that have a specific tag.
        /// </summary>
        /// <param name="tag"> Tag that will be used to fetch data. </param>
        private static void GetKeysByTag(Tag tag)
        {
            //Retrives keys by specified tag
            ICollection keysByTag = _cache.GetKeysByTag(tag);

            if (keysByTag.Count > 0)
            {
                IEnumerator iter = keysByTag.GetEnumerator();
                while (iter.MoveNext())
                {
                    Console.WriteLine(iter.Current.ToString());
                }
            }
        }

        /// <summary>
        /// /// This method fetches keys from the cache that have all the specified tags.
        /// </summary>
        /// <param name="tagList"> Tags that will be used to fetch data. </param>
        private static void GetKeysByMultipleTags(Tag[] tagList)
        {
            //Retrieves those keys only where complete tagList matches
            ICollection keysByEntireTagList = _cache.GetKeysByAllTags(tagList);

            if (keysByEntireTagList.Count > 0)
            {
                IEnumerator iter = keysByEntireTagList.GetEnumerator();
                while (iter.MoveNext())
                {
                    Console.WriteLine(iter.Current.ToString());
                }
            }
        }

        /// <summary>
        /// This method fetches keys from the cache that have any of the specified tag.
        /// </summary>
        /// <param name="tagList"> Tags that will be used to fetch data from the cache. </param>
        private static void GetKeysByAnyTag(Tag[] tagList)
        {
            ICollection keysByAnyTagInList = _cache.GetKeysByAnyTag(tagList);

            if (keysByAnyTagInList.Count > 0)
            {
                IEnumerator iter = keysByAnyTagInList.GetEnumerator();
                while (iter.MoveNext())
                {
                    Console.WriteLine(iter.Current.ToString());
                }
            }
        }

        /// <summary>
        /// This method fetches values from the cache that have a specific tag.
        /// </summary>
        /// <param name="tag"> Tag that will be used to fetch data from the cache. </param>
        private static void GetValuesByTag(Tag tag)
        {
            Hashtable valuesByTag = _cache.GetByTag(tag);

            if (valuesByTag.Count > 0)
            {
                foreach (string key in valuesByTag.Keys)
                {
                    Console.WriteLine("Fetched value by tag of the key : {0}", key);
                }
            }
        }

        /// <summary>
        /// This method fetches values from the cache that have all the specified tags.
        /// </summary>
        /// <param name="tagList"> Tags that will be used to fetch data from the cache. </param>
        private static void GetValuesByMultipleTags(Tag[] tagList)
        {
            Hashtable valuesByEntireTagList = _cache.GetByAllTags(tagList);

            if (valuesByEntireTagList.Count > 0)
            {
                foreach (string key in valuesByEntireTagList.Keys)
                {
                    Console.WriteLine("Fetched value by Entire tag-list of the key : {0}", key);
                }
            }
        }

        /// <summary>
        /// This method fetches values from the cache that have any of the specified tags.
        /// </summary>
        /// <param name="tagList"> Tags that will be used to fetch data from the cache. </param>
        private static void GetValuesByAnyTag(Tag[] tagList)
        {
            Hashtable valuesByAnyTagInList = _cache.GetByAnyTag(tagList);

            if (valuesByAnyTagInList.Count > 0)
            {
                foreach (string key in valuesByAnyTagInList.Keys)
                {
                    Console.WriteLine("Fetched value by any tag of the key : {0}", key);
                }
            }
        }

        /// <summary>
        /// This method removes items from the cache that have a specific tag.
        /// </summary>
        /// <param name="tag"> Tag that will be used to remove data from the cache. </param>
        private static void RemoveItemsByTag(Tag tag)
        {
            _cache.RemoveByTag(tag);

            Console.WriteLine("Items removed by tag.");
        }

        /// <summary>
        /// This method removes items from the cache that have all the specified tags.
        /// </summary>
        /// <param name="tagList"> Tags that will be used to remove data from the cache. </param>
        private static void RemoveItemsByMultipleTags(Tag[] tagList)
        {
            _cache.RemoveByAllTags(tagList);

            Console.WriteLine("Items removed by multiple tags.");
        }

        /// <summary>
        /// This method removes items from the cache that have any of the specified tags.
        /// </summary>
        /// <param name="tagList"> Tags that will be used to remove data from the cache. </param>
        private static void RemoveItemsByAnyTag(Tag[] tagList)
        {
            _cache.RemoveByAnyTag(tagList);

            Console.WriteLine("Items removed by any tag.");
        }
    }
}