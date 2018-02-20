// ===============================================================================
// Alachisoft (R) NCache Sample Code
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Alachisoft.NCache.EntityFrameworkCore;
using Alachisoft.NCache.Samples.NorthwindModels;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that provides the functionality of the sample
    /// </summary>
    class EFCoreCaching
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Run()
        {
            // Initialize the database context
            NorthwindContext database = new NorthwindContext();

            // ==================================================================================================================
            //                                              LINQ QUERY EXTENSION METHODS
            // ==================================================================================================================
            // There are several use cases for the extension methods provided by NCache's EFCore Provider. The extension methods 
            // provided with-hold different strategies (FromCache, LoadIntoCache, FromCacheOnly) and nature (Sync and Async) to 
            // cache queries in EFCore.

            // Working with synchronous extension methods provided by NCache's EFCore Caching Provider
            SyncApproach(database);

            // Working with asynchronous extension methods provided by NCache's EFCore Caching Provider
            AsyncApproach(database);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Relational Data
            // ------------------------------------------------------------------------------------------------------------------
            // Another use case is where queries executed in entity framework contain related entities. NCache's EFCore caching 
            // provider supports such queries and allows the user to cache such result sets as well, along with related entities. 
            // These result sets can be stored as a collection and as seperate entities also. Storing this result set as seperate 
            // entities will only store the parent entities seperately and all the nested entities separately i.e. for an N-level 
            // nesting, only the parent entity will be separate however, the 2nd level entity will nest 2nd level to N-level 
            // entities in it.
            GetRegionInformationWithTerritories(database);

            // ==================================================================================================================
            //                              CACHE LEVEL OPERATIONS / DIRECT CACHE ACCESS METHODS
            // ==================================================================================================================
            // Sometimes in entity framework applications, there is a situation where a handle for a new entity exists. To cache 
            // the entity, LINQ APIs can be used however, these APIs will have to query the database for data that you already 
            // have in your application. Cache level operations provided by NCache's EFCore Caching Provider allow you to deal 
            // with entities at hand without having to go to the database. These operations are of two nature; namely insert and 
            // remove which have been demonstrated in their respective cases below.

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Cache Insert/Update
            // ------------------------------------------------------------------------------------------------------------------
            // To understand this use case, let us assume that a large dataset is stored in the cache against some query. Now if 
            // the data is changed in the database, let us say a new entity is added or an existing entity is modified, the result 
            // set in the cache is invalidated. If a database dependency is registered with that query, the whole result set will 
            // be removed from cache and upon the next execution of that query, all the existing data and modified entity/entities 
            // will have to be fetched from the database again. But as the result set is very large it will have a large cost. 
            // If the user knows the entity that is modified/added, (s)he can insert that entity in cache himself/herself using 
            // the cache update operation. Doing so will again validate the result set and the the cost of loading all the entities 
            // from the database can be saved. Using cache update makes sure that the "CachingOptions" (specially the query 
            // identifier) are the same as the already existing result set. If not, it will cause issues. Note that for this use 
            // case the result set entities should be stored as a seperate entities otherwise it might cause casting exceptions.
            Shippers[] shippers = AddShipperToCache(database);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Cache Remove
            // ------------------------------------------------------------------------------------------------------------------
            // The usage of this method is very similar to the use case descibed in cache insert/update section. The only 
            // difference is that this is used when the entity is removed from the database. Again this will cause the result set 
            // to be invalidated and NCache's EFCore provider exposes the CacheRemove method to remove entities only from the cache
            // thus making the result set valid again. The same limitations are applicable as in cache insert/update section that 
            // the "CachingOptions" (specially the query identifier) are the same as the already existing result set. If not, it 
            // will cause issues. Note that for this use case the result set entities should be stored as a seperate entities 
            // otherwise, it might cause casting exceptions.
            RemoveShipperFromCache(database, shippers[0]);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : RemoveByQueryIdentifier
            // ------------------------------------------------------------------------------------------------------------------
            // The usage of this method is very similar to the use case descibed in cache remove section. The only difference is 
            // that this approach uses query identifier in order to remove entity from cache. The limitations and restrictions in 
            // using this approach are the same as described in the cache remove section.
            RemoveShipperFromCacheViaQueryIdentifier(database, shippers[1]);

            // ==================================================================================================================
            //                                                    NO CACHING
            // ==================================================================================================================
            // There may be cases where caching certain queries may not prove to be beneficial. Such a case has been demonstrated 
            // below.

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Reporting (No Caching)
            // ------------------------------------------------------------------------------------------------------------------
            // There is a use case where user does not intend to cache the query result. A typical use case is for generating 
            // reports. These queries are unique. By unique query we mean that there is a good possibility that this query will 
            // not be executed again and therefore, caching such queries is useless. In general, these queries are complex and 
            // have multiple joins and criteria.
            GenerateOrdersReport(database);

            // ==================================================================================================================
            //                                                   KEY GENERATOR
            // ==================================================================================================================
            // Cache keys for entities are generated internal to NCache's EFCore Caching Provider. For query result sets stored 
            // as collections, the collection gets a unique key, generated from the LINQ expression. This key is returned as an 
            // out parameter from the respective LINQ query extension method call. Cache keys for separate entities cannot be 
            // determined in this manner because the out paramater can only return one key while there may be more than one entity 
            // returned from the call. To determine what key will be generated for an entity, NCache's EFCore Caching Provider 
            // exposes the class that generates the keys. Simply instantiate the class and call on the required method.

            // ------------------------------------------------------------------------------------------------------------------
            // Simple Entities
            // ------------------------------------------------------------------------------------------------------------------
            // Simple entities (entities with no related entities queried) have a very straightforward format for key generation. 
            // It follows the following format,
            //
            //      [FQN of Entity]:[PK Column Name]=[PK Value]
            //
            // Legend,
            //          FQN : Full-Qualified Name
            //          PK  : Primary Key
            //
            PrintKeysForTerritories();

            // ------------------------------------------------------------------------------------------------------------------
            // Nested Entities
            // ------------------------------------------------------------------------------------------------------------------
            // Nested entities have related entities queried with them. The format for such entities have the same format as simple 
            // entities, except that they also put in information from related entities in the resultant key. The format is,
            //
            //      [FQN of Entity]:[PK Column Name]=[PK Value]:[FQN of RE01]:[PK Column Name of RE01]=[PK Value of RE01]...[FQN of RE_N]:[PK Column Name of RE_N]=[PK Value of RE_N]
            //
            // Legend,
            //          FQN : Full-Qualified Name
            //          PK  : Primary Key
            //          RE  : Related Entity
            //
            // It is to be noted that any entities nested in the related entities are also used to generate the key.
            PrintKeysForTerritoriesAndRegions();
        }

        #region --                                            MAIN METHODS                                            --

        /// <summary>
        /// Gets all orders from today and yesterday and prints its details.
        /// </summary>
        /// <param name="database">Instance of database context.</param>
        private static void GenerateOrdersReport(NorthwindContext database)
        {
            // Get all orders from yesterday and today
            var reportQuery = from order in database.Orders
                              where order.OrderDate <= DateTime.Today && order.OrderDate >= DateTime.Today.Subtract(TimeSpan.FromDays(1.0))
                              select order;

            Console.WriteLine("\nReport for orders placed today and yesterday,");
            Console.WriteLine("\n{0,-10} {1,-25} {2,-20}", "Order ID", "Order Date", "Ship Name");
            Console.WriteLine("{0,-10} {1,-25} {2,-20}", new string('-', 10), new string('-', 25), new string('-', 20));

            // Go through each order and print its details
            foreach (Orders order in reportQuery)
            {
                PrintOrderDetail(order);
            }
        }

        /// <summary>
        /// Demonstrates methods pertaining to usage of NCache EFCore Caching Provider's LINQ APIs synchronously.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void SyncApproach(NorthwindContext database)
        {
            // ==================================================================================================================
            //                                              LINQ QUERY EXTENSION METHODS
            // ==================================================================================================================
            // There are several use cases for the extension methods provided by NCache's EFCore Provider. The extension methods 
            // provided with-hold different strategies to cache queries in EFCore. Each use case is explained in detail below,

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : FromCache
            // ------------------------------------------------------------------------------------------------------------------
            // This is a typical use case where different parts of the user application perform the same query. NCache's EFCore 
            // provider exposes an extension method named "FromCache" for such use cases. When the query is executed with 
            // FromCache extension method, the result set is cached on the first execution and upon subsequent calls, the data 
            // is served from the cache provided that the data is still in the cache and is not evicted, expired or invalidated 
            // due to some other reasons (see invalidation section for more details). If data is invalidated, the query is then 
            // executed again on the database and the result set is cached, just as if it was the first execution.
            GetCustomerFromDatabase(database);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : ResultSet Refresher (LoadIntoCache)
            // ------------------------------------------------------------------------------------------------------------------
            // There are certain parts of the user application that are critical and can not work with stale data. To handle 
            // these use cases NCache's EFCore Caching Provider exposes an extension method named "LoadIntoCache". Using this 
            // method with a query will load fresh data from the database irrespective of the fact that it could have been 
            // present in the cache beforehand and could have been served from there. This use case of LoadIntoCache can be 
            // considered as a query result set refresher. An Example where stale data can not be used in the application 
            // could be where an application requires orders for processing billing information.
            GetCustomerOrders(database);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : FromCacheOnly
            // ------------------------------------------------------------------------------------------------------------------
            // This use case is the opposite of "no caching" use case. In no caching use case, the query is executed only on
            // database and totally ignores the cache whereas in this case the query is only executed on the cache and totally 
            // ignores the database. In case if cache is not pre-loaded with data, the query will return default values. And 
            // in case if cache is loaded with partial data, the query will be executed against that data only. From Cache only 
            // is generally used with LoadIntoCache or LoadIntoCacheAsync option, where data is loaded in the cache. An example 
            // of the API is shown in the next use case where pre-loading data is required.

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Referential Data (LoadIntoCache + FromCacheOnly)
            // ------------------------------------------------------------------------------------------------------------------
            // Referential data is that portion of data in the user application that does not change frequently. Ncache's EFCore 
            // provider exposes two extension methods named "LoadIntoCache" and "FromCacheOnly". The purpose of FromCacheOnly is 
            // to query referential data from cache and that of LoadIntoCache is to load the referential data into the cache 
            // while storing it as seperate entities. Doing so will allow the user to perform queries on the cache instead of 
            // the database. Note that all the referential data should be loaded in the cache and not just a portion based on some 
            // criteria as the FromCacheOnly will intercept the call from executing on the database and will translate that call 
            // into NCache's Query (SQL Extended) and will make sure that it is executed on the cache only.
            // An example where referential data can be used is the "products" table. The products information does not change
            // that frequently therefore, user can load all the products in the cache at the application startup and then use 
            // FromCacheOnly API to execute queries on the cache.
            PrintDetailsOfDiscontinuedProducts(database);

            // ==================================================================================================================
            //                                                      DATA INVALIDATION
            // ==================================================================================================================
            // Data invalidation techniques are used in order to work with transactional data. Transactional data is that data 
            // in your application that changes quite frequently. Expiration and database dependency are some common practices 
            // used for this purpose.

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Expiration
            // ------------------------------------------------------------------------------------------------------------------
            // This use case demonstrates one of the methods to inavlidate data from the cache. The most common invalidation
            // option that is used with caching is expiration. Expiration can be used with transcational data that is deemed 
            // less critical and when getting an old state of the data from cache is tolerable.
            // NCache's EFCore provider presents caching options in which user can either set Absolute expiration or sliding 
            // expiration.
            // The purpose of absolute expiration is to make sure after a certain time data is removed from the cache so that 
            // upon next execution of that query, it is fetched from the database again so that any modification in the database 
            // can be reflected in the cache as well.
            // Sliding expiration is the second type of expiration that NCache's EFCore provider presents. This can be used 
            // to make sure the cache is not occupied with the data that is not used. Any data that is not fetched in a certain
            // time period is removed from the cache so that the cache can be made available for data that is actually being 
            // used by the client application.
            GetSupplierInformation(database);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Database Dependency
            // ------------------------------------------------------------------------------------------------------------------
            // This use case demonstrates another method to invalidate data from the cache. Database dependency is generally 
            // used with result sets that are critical and any modification of such data in the database should be reflected 
            // in the cache as soon as possible.
            // The user has to set dbdependency flag of such queries to true. Doing so will use the NCache's DBDependency 
            // feature and will register the query in the database for tracking any modification. When the data that falls 
            // uder that query criteria is modified NCache removes the data from the cache, therefore when the same query is 
            // exectued again it does not find the data in the cache and has to execute it on the database thus providing
            // fresh data to the user application. Note that DBDependency is only available for Oracle and SQL Server database 
            // systems.
            GetRegionInformation(database);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Eviction
            // ------------------------------------------------------------------------------------------------------------------
            // Another data invalidation option is eviction. This is a cache level option. If the cache is configured to perform
            // eviction when it gets full, some of the data is removed from the cache, so that space can be made for the coming 
            // result sets. There are different eviction options such as Least Frequently used (LFU), least recently used (LRU) 
            // and priority. NCache's EFCore provider presents caching options so that the user can set the priority of the result
            // sets. When eviction triggers the results sets with the lowest priority are removed first and onwards until the 
            // required percentage of cache space is free. Eviction is not triggered from code but from within the cache itself 
            // when cache size is close to limit.

            // ==================================================================================================================
            //                                          AGGREGATE OPERATION CACHING
            // ==================================================================================================================
            // NCache's EFCore Caching Provider also provides functionality of caching results of aggregate operations. For this, 
            // Deferred* APIs have been provided. It is to be noted that caching for aggregate operations should be done very
            // carefully. Results of aggregate operations are always stored as collections and hence cannot be fetched via the 
            // FromCacheOnly API and to fetch the result of a cached aggregate operation, the query generated should be exactly 
            // the same as when generated to cache the result. This means the same LINQ expression has to be used to fetch the 
            // cached result again.

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Element Operators
            // ------------------------------------------------------------------------------------------------------------------
            // This use case handles caching results of aggregate operations that return entities. For this Deferred* APIs are 
            // used which cache only the entity and the limitations and restrictions to this practice are the same as discussed 
            // above.
            GetFirstShipperInstance(database);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Miscellaneous LINQ Operators
            // ------------------------------------------------------------------------------------------------------------------
            // This use case handles caching results of aggregate operations that return values. For this Deferred* APIs are 
            // used which cache only the value results and the limitations and restrictions to this practice are the same as 
            // discussed above.
            GetTotalShippersCount(database);
        }

        /// <summary>
        /// Demonstrates methods pertaining to usage of NCache EFCore Caching Provider's LINQ APIs asynchronously.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void AsyncApproach(NorthwindContext database)
        {
            // ==================================================================================================================
            //                                              LINQ QUERY EXTENSION METHODS
            // ==================================================================================================================
            // There are several use cases for the extension methods provided by NCache's EFCore Provider. The extension methods 
            // provided with-hold different strategies to cache queries in EFCore. Each use case is explained in detail below,

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : FromCacheAsync
            // ------------------------------------------------------------------------------------------------------------------
            // This is a typical use case where different parts of the user application perform the same query. NCache's EFCore 
            // provider exposes an extension method named "FromCacheAsync" for such use cases. When the query is executed with 
            // FromCacheAsync extension method, the result set is cached on the first (async) execution and upon subsequent calls, 
            // the data is served from the cache provided that the data is still in the cache and is not evicted, expired or 
            // invalidated due to some other reasons (see invalidation section for more details). If data is invalidated, the 
            // query is then executed again on the database and the result set is cached, just as if it was the first execution.
            GetCustomerFromDatabaseAsync(database);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : ResultSet Refresher (LoadIntoCacheAsync)
            // ------------------------------------------------------------------------------------------------------------------
            // There are certain parts of the user application that are critical and can not work with stale data. To handle 
            // these use cases NCache's EFCore Caching Provider exposes an async extension method named "LoadIntoCacheAsync". 
            // Using this method with a query will load fresh data from the database asynchronously irrespective of the fact 
            // that it could have been present in the cache beforehand and could have been served from there. This use case of 
            // LoadIntoCacheAsync can be considered as a query result set refresher. An Example where stale data can not be 
            // used in the application could be where an application requires orders for processing billing information.
            GetCustomerOrdersAsync(database);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : FromCacheOnlyAsync
            // ------------------------------------------------------------------------------------------------------------------
            // This use case is the opposite of "no caching" use case. In no caching use case, the query is executed only on
            // database and totally ignores the cache whereas in this case the query is only executed on the cache and totally 
            // ignores the database. In case if cache is not pre-loaded with data, the query will return default values. And 
            // in case if cache is loaded with partial data, the query will be executed against that data only. FromCacheOnlyAsync 
            // is generally used with LoadIntoCache or LoadIntoCacheAsync option, where data is loaded in the cache. An example of 
            // the API is shown in the next use case where pre-loading data is required.

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Referential Data (LoadIntoCacheAsync + FromCacheOnlyAsync)
            // ------------------------------------------------------------------------------------------------------------------
            // Referential data is that portion of data in the user application that does not change frequently. Ncache's EFCore 
            // provider exposes two extension methods named "LoadIntoCacheAsync" and "FromCacheOnlyAsync". The purpose of 
            // FromCacheOnlyAsync is to query referential data from cache asynchronously and that of LoadIntoCacheAsync is to load 
            // the referential data into the cache asynchronously while storing it as seperate entities. Doing so will allow the 
            // user to perform queries on the cache instead of the database. Note that all the referential data should be loaded 
            // in the cache and not just a portion based on some criteria as the FromCacheOnlyAsync will intercept the call from 
            // executing on the database and will translate that call into NCache's Query (SQL Extended) and will make sure that 
            // it is executed on the cache only.
            // An example where referential data can be used is the "products" table. The products information does not change
            // that frequently therefore, user can load all the products in the cache at the application startup and then use 
            // FromCacheOnlyAsync API to execute queries on the cache asynchronously.
            PrintDetailsOfDiscontinuedProductsAsync(database);

            // ==================================================================================================================
            //                                                      DATA INVALIDATION
            // ==================================================================================================================
            // Data invalidation techniques are used in order to work with transactional data. Transactional data is that data 
            // in your application that changes quite frequently. Expiration and database dependency are some common practices 
            // used for this purpose.

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Expiration
            // ------------------------------------------------------------------------------------------------------------------
            // This use case demonstrates one of the methods to inavlidate data from the cache. The most common invalidation
            // option that is used with caching is expiration. Expiration can be used with transcational data that is deemed 
            // less critical and when getting an old state of the data from cache is tolerable.
            // NCache's EFCore provider presents caching options in which user can either set Absolute expiration or sliding 
            // expiration.
            // The purpose of absolute expiration is to make sure after a certain time data is removed from the cache so that 
            // upon next execution of that query, it is fetched from the database again so that any modification in the database 
            // can be reflected in the cache as well.
            // Sliding expiration is the second type of expiration that NCache's EFCore provider presents. This can be used 
            // to make sure the cache is not occupied with the data that is not used. Any data that is not fetched in a certain
            // time period is removed from the cache so that the cache can be made available for data that is actually being 
            // used by the client application.
            GetSupplierInformationAsync(database);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Database Dependency
            // ------------------------------------------------------------------------------------------------------------------
            // This use case demonstrates another method to invalidate data from the cache. Database dependency is generally 
            // used with result sets that are critical and any modification of such data in the database should be reflected 
            // in the cache as soon as possible.
            // The user has to set dbdependency flag of such queries to true. Doing so will use the NCache's DBDependency 
            // feature and will register the query in the database for tracking any modification. When the data that falls 
            // uder that query criteria is modified NCache removes the data from the cache, therefore when the same query is 
            // exectued again it does not find the data in the cache and has to execute it on the database thus providing
            // fresh data to the user application. Note that DBDependency is only available for Oracle and SQL Server database 
            // systems.
            GetRegionInformationAsync(database);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Eviction
            // ------------------------------------------------------------------------------------------------------------------
            // Another data invalidation option is eviction. This is a cache level option. If the cache is configured to perform
            // eviction when it gets full, some of the data is removed from the cache, so that space can be made for the coming 
            // result sets. There are different eviction options such as Least Frequently used (LFU), least recently used (LRU) 
            // and priority. NCache's EFCore provider presents caching options so that the user can set the priority of the result
            // sets. When eviction triggers the results sets with the lowest priority are removed first and onwards until the 
            // required percentage of cache space is free. Eviction is not triggered from code but from within the cache itself 
            // when cache size is close to limit.

            // ==================================================================================================================
            //                                          AGGREGATE OPERATION CACHING
            // ==================================================================================================================
            // NCache's EFCore Caching Provider also provides functionality of caching results of aggregate operations. For this, 
            // Deferred* APIs have been provided. It is to be noted that caching for aggregate operations should be done very
            // carefully. Results of aggregate operations are always stored as collections and hence cannot be fetched via the 
            // FromCacheOnly API and to fetch the result of a cached aggregate operation, the query generated should be exactly 
            // the same as when generated to cache the result. This means the same LINQ expression has to be used to fetch the 
            // cached result again.

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Element Operators
            // ------------------------------------------------------------------------------------------------------------------
            // This use case handles caching results of aggregate operations that return entities. For this Deferred* APIs are 
            // used which cache only the entity and the limitations and restrictions to this practice are the same as discussed 
            // above.
            GetFirstShipperInstanceAsync(database);

            // ------------------------------------------------------------------------------------------------------------------
            // Use Case : Miscellaneous LINQ Operators
            // ------------------------------------------------------------------------------------------------------------------
            // This use case handles caching results of aggregate operations that return values. For this Deferred* APIs are 
            // used which cache only the value results and the limitations and restrictions to this practice are the same as 
            // discussed above.
            GetTotalShippersCountAsync(database);
        }

        #region --                                              SYNC APIs                                             --

        /// <summary>
        /// Gets Customer from database and also stores it into cache.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void GetCustomerFromDatabase(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                QueryIdentifier = null,
                CreateDbDependency = false,
                StoreAs = StoreAs.Collection,
                Priority = Runtime.CacheItemPriority.Default
            };
            string cacheKey;
            var customerQuery = (from customerDetail in database.Customers
                                 where customerDetail.CustomerId == "TOMSP"
                                 select customerDetail)
                                 .FromCache(out cacheKey, options).AsQueryable();
            PrintCustomerDetail(customerQuery);
        }

        /// <summary>
        /// Get Customer's orders from database
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void GetCustomerOrders(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                QueryIdentifier = null,
                CreateDbDependency = false,
                StoreAs = StoreAs.Collection,
                Priority = Runtime.CacheItemPriority.Default
            };
            string cacheKey;
            var orderQuery = (from customerOrder in database.Orders
                              where customerOrder.Customer.CustomerId == "TOMSP"
                              select customerOrder)
                             .LoadIntoCache(out cacheKey, options);
            PrintCustomerOrders(orderQuery, "TOMSP");
        }

        /// <summary>
        /// Prints details of all discontinued products in database.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void PrintDetailsOfDiscontinuedProducts(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                QueryIdentifier = null,
                CreateDbDependency = false,
                StoreAs = StoreAs.SeperateEntities,
                Priority = Runtime.CacheItemPriority.Default
            };

            // Load all products into cache as individual objects
            (from products in database.Products select products).LoadIntoCache(options).ToList();

            // Query cache for any products that are discontinued
            var discontinuedProducts = (from product in database.Products
                                        where product.Discontinued == true
                                        select product)
                                        .FromCacheOnly().ToList();

            Console.WriteLine("\n{0, -20} {1, -50} {2, -20}", "Product ID", "Product Name", "Quantity Per Unit");
            Console.WriteLine("{0, -20} {1, -50} {2, -20}", new string('-', 20), new string('-', 50), new string('-', 20));

            // Go through all the discontinued products
            // and print their details
            foreach (Products product in discontinuedProducts)
            {
                PrintProductDetails(product);
            }
        }

        /// <summary>
        /// Gets all of customer's orders and prints their details.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void GetRegionInformationWithTerritories(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                StoreAs = StoreAs.SeperateEntities
            };

            var regionDetails = (from region in database.Region select region)
                                .Include(region => region.Territories)
                                .FromCache(options)
                                .ToList();

            Console.WriteLine("\n{0,-20} {1,-20} {2,-20} {3,-20}", "Region ID", "Region Name", "Territory ID", "Territory Name");
            Console.WriteLine("{0,-20} {1,-20} {2,-20} {3,-20}", new string('-', 20), new string('-', 20), new string('-', 20), new string('-', 20));

            foreach (Region region in regionDetails)
            {
                PrintRegionDetails(region);
            }
        }

        /// <summary>
        /// Gets information of suppliers from database if not found in cache and prints its details.
        /// Along with caching the supplier information, this method also adds expiration (both absolute
        /// and sliding) with the entities cached. This is why the data may be fetched from database on 
        /// successive calls since data may be expired by the time this method is called again.
        /// </summary>
        /// <param name="database"></param>
        private static void GetSupplierInformation(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                StoreAs = StoreAs.Collection
            };
            // Set absolute expiration of item to be the time 1 hour from now
            options.SetAbsoluteExpiration(DateTime.Now.AddHours(1.0));

            // Load suppliers into the cache with the expiration options
            var suppliers = (from supplier in database.Suppliers
                             where supplier.SupplierId < 10
                             select supplier)
                             .FromCache(options);

            Console.WriteLine("\n{0,-20} {1,-40} {2,-20} {3,-20}", "Supplier ID", "Company Name", "Phone Number", "Postal Code");
            Console.WriteLine("{0,-20} {1,-40} {2,-20} {3,-20}", new string('-', 20), new string('-', 40), new string('-', 20), new string('-', 20));

            // Go through each supplier and print its details
            foreach (Suppliers supplier in suppliers)
            {
                PrintSupplierInformation(supplier);
            }

            // Set sliding expiration of the item to be for 1 hour
            options.SetSlidingExpiration(new TimeSpan(1, 0, 0));

            var moreSuppliers = (from supplier in database.Suppliers
                                 where supplier.SupplierId >= 10 && supplier.SupplierId <= 20
                                 select supplier)
                                .FromCache(options);

            // Go through each supplier and print its details
            foreach (Suppliers supplier in moreSuppliers)
            {
                PrintSupplierInformation(supplier);
            }
        }

        /// <summary>
        /// Gets regions and all associated territories and prints their details.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void GetRegionInformation(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                CreateDbDependency = true,
                StoreAs = StoreAs.Collection
            };

            // Get region with corresponding territories
            var regionDetails = (from region in database.Region select region)
                                .Include(region => region.Territories)
                                .FromCache(options);

            Console.WriteLine("\n{0,-20} {1,-20} {2,-20} {3,-20}", "Region ID", "Region Name", "Territory ID", "Territory Name");
            Console.WriteLine("{0,-20} {1,-20} {2,-20} {3,-20}", new string('-', 20), new string('-', 20), new string('-', 20), new string('-', 20));

            foreach (Region region in regionDetails)
            {
                PrintRegionDetails(region);
            }
        }

        /// <summary>
        /// Prints the company name of the first shipper in database while also caching the result.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void GetFirstShipperInstance(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                StoreAs = StoreAs.Collection    // For aggregate operations, StoreAs is always 
                                                // overridden with StoreAs.Collection value
            };

            // Run query
            string cacheKey;
            var shipper = database.Shippers.DeferredFirstOrDefault().FromCache(out cacheKey, options);

            // Print result
            Console.WriteLine($"\nTop-most shipper in database belongs to {shipper.CompanyName} company.");
        }

        /// <summary>
        /// Gets the total number of shippers in the database and caches the result.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void GetTotalShippersCount(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                StoreAs = StoreAs.Collection    // For aggregate operations, StoreAs is always 
                                                // overridden with StoreAs.Collection value
            };

            // Run query
            string cacheKey;
            var count = database.Shippers.DeferredCount().FromCache(out cacheKey, options);

            // Print result
            Console.WriteLine($"\nDatabase contains information of {count} shippers.");
        }

        #endregion

        #region --                                              ASYNC APIs                                            --

        /// <summary>
        /// Gets Customer from database and also stores it into cache.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void GetCustomerFromDatabaseAsync(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                QueryIdentifier = null,
                CreateDbDependency = false,
                StoreAs = StoreAs.Collection,
                Priority = Runtime.CacheItemPriority.Default
            };

            // Invoke the async extension method
            var task = (from customerDetail in database.Customers
                        where customerDetail.CustomerId == "TOMSP"
                        select customerDetail)
                        .FromCacheAsync(options);

            // Perform some other task until this one is complete
            TaskExhaustor(task);

            // Get the result
            IQueryable<Customers> result = task.Result.AsQueryable();

            // Print the details from it
            PrintCustomerDetail(result);
        }

        /// <summary>
        /// Get Customer's orders from database
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void GetCustomerOrdersAsync(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                QueryIdentifier = null,
                CreateDbDependency = false,
                StoreAs = StoreAs.Collection,
                Priority = Runtime.CacheItemPriority.Default
            };

            // Invoke the async extension method
            var task = (from customerOrder in database.Orders
                        where customerOrder.Customer.CustomerId == "TOMSP"
                        select customerOrder)
                        .LoadIntoCacheAsync(options);

            // Perform some other task until this one is complete
            TaskExhaustor(task);

            // Get the result
            IQueryable<Orders> result = task.Result.AsQueryable();

            // Print the details from it
            PrintCustomerOrders(result, "TOMSP");
        }

        /// <summary>
        /// Prints details of all discontinued products in database.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void PrintDetailsOfDiscontinuedProductsAsync(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                QueryIdentifier = null,
                CreateDbDependency = false,
                StoreAs = StoreAs.SeperateEntities,
                Priority = Runtime.CacheItemPriority.Default
            };

            // Load all products into cache as individual objects asynchronously
            var loadTask = (from products in database.Products select products).LoadIntoCacheAsync(options);

            // Perform some other task until this is done
            TaskExhaustor(loadTask);

            // Query cache for any products that are discontinued
            var fetchTask = (from product in database.Products
                             where product.Discontinued == true
                             select product)
                            .FromCacheOnlyAsync();

            // Perform some other task until this is done
            TaskExhaustor(fetchTask);

            Console.WriteLine("\n{0, -20} {1, -50} {2, -20}", "Product ID", "Product Name", "Quantity Per Unit");
            Console.WriteLine("{0, -20} {1, -50} {2, -20}", new string('-', 20), new string('-', 50), new string('-', 20));

            // Get the result of query executed asycnhronously
            IList<Products> resultantProducts = fetchTask.Result.ToList();

            // Go through all the discontinued products
            // and print their details
            foreach (Products product in resultantProducts)
            {
                PrintProductDetails(product);
            }
        }

        /// <summary>
        /// Gets information of suppliers from database if not found in cache and prints its details.
        /// Along with caching the supplier information, this method also adds expiration (both absolute
        /// and sliding) with the entities cached. This is why the data may be fetched from database on 
        /// successive calls since data may be expired by the time this method is called again.
        /// </summary>
        /// <param name="database"></param>
        private static void GetSupplierInformationAsync(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                StoreAs = StoreAs.Collection
            };
            // Set absolute expiration of item to be the time 1 hour from now
            options.SetAbsoluteExpiration(DateTime.Now.AddHours(1.0));

            // Load suppliers into the cache with the expiration options asynchronously
            var suppliersTask = (from supplier in database.Suppliers
                                 where supplier.SupplierId < 10
                                 select supplier)
                                .FromCacheAsync(options);

            // Perform some other task while this is executed
            TaskExhaustor(suppliersTask);

            Console.WriteLine("\n{0,-20} {1,-40} {2,-20} {3,-20}", "Supplier ID", "Company Name", "Phone Number", "Postal Code");
            Console.WriteLine("{0,-20} {1,-40} {2,-20} {3,-20}", new string('-', 20), new string('-', 40), new string('-', 20), new string('-', 20));

            // Get the result of execution
            IList<Suppliers> suppliers = suppliersTask.Result.ToList();

            // Go through each supplier and print its details
            foreach (Suppliers supplier in suppliers)
            {
                PrintSupplierInformation(supplier);
            }

            // Set sliding expiration of the item to be for 1 hour
            options.SetSlidingExpiration(new TimeSpan(1, 0, 0));

            // Fetch more suppliers with new caching options asynchronously
            var moreSuppliersTask = (from supplier in database.Suppliers
                                     where supplier.SupplierId >= 10 && supplier.SupplierId <= 20
                                     select supplier)
                                    .FromCacheAsync(options);

            // Perform some other task while this is executed
            TaskExhaustor(moreSuppliersTask);

            // Get the result of execution
            IList<Suppliers> moreSuppliers = moreSuppliersTask.Result.ToList();

            // Go through each supplier and print its details
            foreach (Suppliers supplier in moreSuppliers)
            {
                PrintSupplierInformation(supplier);
            }
        }

        /// <summary>
        /// Gets regions and all associated territories and prints their details.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void GetRegionInformationAsync(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                CreateDbDependency = true,
                StoreAs = StoreAs.Collection
            };

            // Get region with corresponding territories asynchronously
            var task = (from region in database.Region select region)
                        .Include(region => region.Territories)
                        .FromCacheAsync(options);

            // Perform some other task while this is completed
            TaskExhaustor(task);

            Console.WriteLine("\n{0,-20} {1,-20} {2,-20} {3,-20}", "Region ID", "Region Name", "Territory ID", "Territory Name");
            Console.WriteLine("{0,-20} {1,-20} {2,-20} {3,-20}", new string('-', 20), new string('-', 20), new string('-', 20), new string('-', 20));

            // Get the result of execution
            IList<Region> regions = task.Result.ToList();

            // Print the required details
            foreach (Region region in regions)
            {
                PrintRegionDetails(region);
            }
        }

        /// <summary>
        /// Prints the company name of the first shipper in database while also caching the result
        /// via async approach.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void GetFirstShipperInstanceAsync(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                StoreAs = StoreAs.Collection    // For aggregate operations, StoreAs is always 
                                                // overridden with StoreAs.Collection value
            };

            // Run async query
            var task = database.Shippers.DeferredFirstOrDefault().FromCacheAsync(options);

            // Perform some other task while this operation executes
            TaskExhaustor(task);

            // Get the result
            Shippers shipper = task.Result;

            // Print the result
            Console.WriteLine($"\nTop-most shipper in database belongs to {shipper.CompanyName} company.");
        }

        /// <summary>
        /// Gets the total number of shippers in the database and caches the result via async approach.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void GetTotalShippersCountAsync(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                StoreAs = StoreAs.Collection    // For aggregate operations, StoreAs is always 
                                                // overridden with StoreAs.Collection value
            };

            // Run the async query
            var task = database.Shippers.DeferredCount().FromCacheAsync(options);

            // Preform some other task while this is completed
            TaskExhaustor(task);

            // Get the result
            int count = task.Result;

            // Print the result
            Console.WriteLine($"\nDatabase contains information of {count} shippers.");
        }

        #endregion

        /// <summary>
        /// Adds a new shipper from company "Panda Express" into the database and cache using cache only operations.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        /// <returns>The new shipper instance</returns>
        private static Shippers[] AddShipperToCache(NorthwindContext database)
        {
            CachingOptions options = new CachingOptions
            {
                StoreAs = StoreAs.SeperateEntities
            };

            // Instantiate a new shipper instances
            Shippers shipperPandaExpress = new Shippers()
            {
                CompanyName = "Panda Express",
                Phone = "(503) 555-1234"
            };
            Shippers shipperTitanic = new Shippers()
            {
                CompanyName = "Titanic",
                Phone = "(503) 555-5678"
            };

            // Add them to the database
            database.Shippers.Add(shipperPandaExpress);
            database.Shippers.Add(shipperTitanic);
            database.SaveChanges();

            string cacheKey;

            // Add them to cache (without querying the database)
            Cache cache = database.GetCache();
            cache.Insert(shipperPandaExpress, out cacheKey, options);

            // Update caching options and set value for query identifier
            options.QueryIdentifier = new Runtime.Caching.Tag($"{shipperTitanic.GetType().FullName}:{shipperTitanic.CompanyName}");

            // Insert this new entity into cache with updated caching 
            // options where query identifier is set
            cache.Insert(shipperTitanic, out cacheKey, options);

            return new Shippers[] { shipperPandaExpress, shipperTitanic };
        }

        /// <summary>
        /// Removes the <paramref name="shipper"/> instance passed to this method from the database and 
        /// cache using cache only operations.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        /// <param name="shipper">The <seealso cref="Shippers"/> instance to be removed</param>
        private static void RemoveShipperFromCache(NorthwindContext database, Shippers shipper)
        {
            CachingOptions options = new CachingOptions
            {
                StoreAs = StoreAs.SeperateEntities
            };

            // Remove the shipper from database
            database.Shippers.Remove(shipper);
            database.SaveChanges();

            // Remove it from cache without querying the database
            Cache cache = database.GetCache();
            cache.Remove(shipper);
        }

        /// <summary>
        /// Removes the <paramref name="shipper"/> instances from the database and cache using cache only 
        /// operations with query identifier.
        /// </summary>
        /// <param name="database">Instance of database context</param>
        private static void RemoveShipperFromCacheViaQueryIdentifier(NorthwindContext database, Shippers shipper)
        {
            CachingOptions options = new CachingOptions
            {
                StoreAs = StoreAs.SeperateEntities
            };

            // Remove the shipper from database
            database.Shippers.Remove(shipper);
            database.SaveChanges();

            // Remove it from cache without querying the database and via query identifier
            Cache cache = database.GetCache();
            cache.RemoveByQueryIdentifier(
                new Runtime.Caching.Tag($"{shipper.GetType().FullName}:{shipper.CompanyName}")
            );
        }

        /// <summary>
        /// Prints cache keys generated for <seealso cref="Territories"/> entities queried.
        /// A new database context has been instantiated within this method to make sure 
        /// entities returned do not have related entities in them because of state manager 
        /// (from EF).
        /// </summary>
        /// <param name="database">Instance of database context.</param>
        private static void PrintKeysForTerritories()
        {
            using (NorthwindContext database = new NorthwindContext())
            {
                string message = "Keys generated for territory entities,";

                Console.WriteLine($"\n{message}");
                Console.WriteLine(new string('-', message.Length));

                var entities = (from territory in database.Territories
                                select territory)
                               .Take(5);

                IEnumerator<Territories> enumerator = entities.GetEnumerator();

                DefaultKeyGenerator keyGenerator = new DefaultKeyGenerator();

                while (enumerator.MoveNext())
                {
                    Territories entity = enumerator.Current;

                    string keyGenerated = keyGenerator.GetKey(database, entity);

                    Console.WriteLine($"Key : {keyGenerated}");
                }
            }
        }

        /// <summary>
        /// Prints cache keys generated for <seealso cref="Territories"/> entities queried
        /// with <seealso cref="Region"/> related entities. A new database context has been 
        /// instantiated within this method to work with fresh data from database rather 
        /// than the data from state manager (from EF).
        /// </summary>
        /// <param name="database">Instance of database context.</param>
        private static void PrintKeysForTerritoriesAndRegions()
        {
            using (NorthwindContext database = new NorthwindContext())
            {
                string message = "Keys generated for territory entities with related region entities,";

                Console.WriteLine($"\n{message}");
                Console.WriteLine(new string('-', message.Length));

                var entities = (from territory in database.Territories
                                select territory)
                               .Include(t => t.Region)
                               .Take(5);

                IEnumerator<Territories> enumerator = entities.GetEnumerator();

                DefaultKeyGenerator keyGenerator = new DefaultKeyGenerator();

                while (enumerator.MoveNext())
                {
                    Territories entity = enumerator.Current;

                    string keyGenerated = keyGenerator.GetKey(database, entity);

                    Console.WriteLine($"Key : {keyGenerated}");
                }
            }
        }

        #endregion

        #region --                                          HELPING METHODS                                           --

        /// <summary>
        /// Display customer details
        /// </summary>
        /// <param name="list">List of customers of whom the details are to be printed</param>
        private static void PrintCustomerDetail(IQueryable<Customers> list)
        {
            foreach (Customers customer in list)
            {
                if (customer != null)
                {
                    Console.WriteLine("\nCustomer's Detail");
                    Console.WriteLine("-----------------");

                    Console.WriteLine("Customer ID : " + customer.CustomerId);
                    Console.WriteLine("Name        : " + customer.ContactName);
                    Console.WriteLine("Company     : " + customer.CompanyName);
                    Console.WriteLine("Address     : " + customer.Address);
                }
            }
        }

        /// <summary>
        /// Display customer orders
        /// </summary>
        /// <param name="list">List containing the order instances</param>
        /// <param name="id">The CustomerId of the customer to whom the orders belong to</param>
        private static void PrintCustomerOrders(IEnumerable<Orders> list, string id)
        {
            IEnumerator ie = list.GetEnumerator();
            ie.MoveNext();

            Orders selectedOrder = (Orders)ie.Current;

            if (selectedOrder != null)
            {
                if (selectedOrder.Customer != null)
                {
                    Console.WriteLine("\n" + selectedOrder.Customer.ContactName + "'s Orders");
                }
                else
                {
                    NorthwindContext database = new NorthwindContext();

                    CachingOptions options = new CachingOptions
                    {
                        QueryIdentifier = null,
                        CreateDbDependency = false,
                        StoreAs = StoreAs.Collection,
                        Priority = Runtime.CacheItemPriority.Default
                    };

                    string cacheKey;
                    IQueryable<Customers> customerQuery = database.Customers.AsQueryable<Customers>();
                    customerQuery = customerQuery.Where(customer => customer.CustomerId == id)
                                    .FromCache(out cacheKey, options).AsQueryable();

                    IEnumerator enumerator = customerQuery.GetEnumerator();
                    enumerator.MoveNext();

                    Customers cust = (Customers)enumerator.Current;

                    Console.WriteLine("\n" + cust.CustomerId + "'s Orders");
                }
                Console.WriteLine("------------------------");
                Console.WriteLine(" {0,-10} {1,-25} {2,-20}", "Order ID", "Order Date", "Ship Name");
                Console.WriteLine(" {0,-10} {1,-25} {2,-20}", "--------", "-----------", "---------");

                PrintOrderDetail(selectedOrder);
            }

            while (ie.MoveNext())
            {
                if (ie.Current != null)
                {
                    PrintOrderDetail((Orders)ie.Current);
                }
            }
        }

        /// <summary>
        /// Display order details
        /// </summary>
        /// <param name="order">The order instance of whom the details are to be printed</param>
        private static void PrintOrderDetail(Orders order)
        {
            if (order != null)
                Console.WriteLine(" {0,-10} {1,-25} {2,-20}", order.OrderId, order.OrderDate.ToString(), order.ShipName);
        }

        /// <summary>
        /// Prints details of <paramref name="product"/> instance passed.
        /// </summary>
        /// <param name="product">The instance of <see cref="Products"/> whose details are to be printed.</param>
        private static void PrintProductDetails(Products product)
        {
            if (product != null)
            {
                Console.WriteLine("{0, -20} {1, -50} {2, -20}", product.ProductId, product.ProductName, product.QuantityPerUnit);
            }
        }

        /// <summary>
        /// Prints information of <paramref name="supplier"/> instance passed.
        /// </summary>
        /// <param name="supplier">The instance of supplier whose information is to be shown.</param>
        private static void PrintSupplierInformation(Suppliers supplier)
        {
            if (supplier != null)
            {
                Console.WriteLine("{0,-20} {1,-40} {2,-20} {3,-20}", supplier.SupplierId, supplier.CompanyName, supplier.Phone, supplier.PostalCode);
            }
        }

        /// <summary>
        /// Prints the <paramref name="region"/> information along with the associated territories.
        /// </summary>
        /// <param name="region">The instance of <seealso cref="Region"/> whose details are to be printed.</param>
        private static void PrintRegionDetails(Region region)
        {
            if (region != null)
            {
                Console.WriteLine("{0,-20} {1,-20}", region.RegionId, region.RegionDescription.Trim());

                foreach (Territories territory in region.Territories)
                {
                    Console.WriteLine("{0,-20} {1,-20} {2,-20} {3,-20}", string.Empty, string.Empty, territory.TerritoryId, territory.TerritoryDescription);
                }
            }
        }

        /// <summary>
        /// The purpose of this method is to emulate a secondary operation that a user may 
        /// wish to perform while their task is executing.
        /// </summary>
        /// <typeparam name="T">Since this method has to exhaust all kinds of tasks, therefore, 
        /// it has been made generic to cater to all of them.</typeparam>
        /// <param name="task">Instance of task that is to be exhausted.</param>
        private static void TaskExhaustor<T>(Task<T> task)
        {
            task.Wait();
        }

        #endregion
    }
}
