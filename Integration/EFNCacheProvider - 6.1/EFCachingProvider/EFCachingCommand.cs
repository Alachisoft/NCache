// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.Globalization;
using System.Text;
using System.Threading;

using Alachisoft.NCache.Integrations.EntityFramework.Toolkit;
using Alachisoft.NCache.Integrations.EntityFramework.CacheEntry;
using Alachisoft.NCache.Integrations.EntityFramework.Config;
using Alachisoft.NCache.Integrations.EntityFramework.Util;
using Alachisoft.NCache.Integrations.EntityFramework.Caching;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Toolkit;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Common;

namespace Alachisoft.NCache.Integrations.EntityFramework
{
    /// <summary>
    /// Implementation of <see cref="DbCommand"/> wrappr which implements query caching.
    /// </summary>
    public sealed class EFCachingCommand : DbCommandWrapperExtended
    {
        private static int cacheableCommands;
        private static int nonCacheableCommands;
        private static int cacheHits;
        private static int cacheMisses;
        private static int cacheAdds;

        /// <summary>
        /// Initializes a new instance of the EFCachingCommand class.
        /// </summary>
        /// <param name="wrappedCommand">The wrapped command.</param>
        /// <param name="commandDefinition">The command definition.</param>
        public EFCachingCommand(System.Data.Common.DbCommand wrappedCommand, EFCachingCommandDefinition commandDefinition)
            : base(wrappedCommand, commandDefinition)
        {
        }

        /// <summary>
        /// Gets the number of cacheable commands.
        /// </summary>
        /// <value>The cacheable commands.</value>
        public static int CacheableCommands
        {
            get { return cacheableCommands; }
        }

        /// <summary>
        /// Gets the number of non-cacheable commands.
        /// </summary>
        /// <value>The non cacheable commands.</value>
        public static int NonCacheableCommands
        {
            get { return nonCacheableCommands; }
        }

        /// <summary>
        /// Gets the number of cache hits.
        /// </summary>
        /// <value>The cache hits.</value>
        public static int CacheHits
        {
            get { return cacheHits; }
        }

        /// <summary>
        /// Gets the total number of cache misses.
        /// </summary>
        public static int CacheMisses
        {
            get { return cacheMisses; }
        }

        /// <summary>
        /// Gets the total number of cache adds.
        /// </summary>
        /// <value>The number of cache adds.</value>
        public static int CacheAdds
        {
            get { return cacheAdds; }
        }     

        /// <summary>
        /// Executes the command text against the connection.
        /// </summary>
        /// <param name="behavior">An instance of <see cref="T:System.Data.CommandBehavior"/>.</param>
        /// <returns>
        /// A <see cref="T:System.Data.Common.DbDataReader"/>.
        /// </returns>
        protected override System.Data.Common.DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            Query query= Query.CreateQuery(this.WrappedCommand, this.Definition.IsStoredProcedure);

            ICache cache = Application.Instance.Cache;

            DbResultItem item = new DbResultItem();
            item.ConnectionString = this.Connection.ConnectionString;
            List<string> parameterList=null;
            bool cacheable = Application.Instance.CachePolicy.GetEffectivePolicy(query.QueryText, out item.AbsoluteExpiration,
                out item.SlidingExpiration, out item.TargetDatabase, out item.DbSyncDependency,out parameterList);

            ///No dependency for stored procedures
            if (this.Definition.IsStoredProcedure)
            {
                item.DbSyncDependency = false;
            }

            if (cache == null || query.QueryText == null || !this.Definition.IsCacheable() || !cacheable)
            {
                Interlocked.Increment(ref nonCacheableCommands);
                return WrappedCommand.ExecuteReader(behavior);
            }

            object value = null;

            Interlocked.Increment(ref cacheableCommands);
            
            string cacheKey = query.GetCacheKey(parameterList, this.Parameters);

            if (cache.GetItem(cacheKey, out value))
            {
                Interlocked.Increment(ref cacheHits);

                // got cache entry - create reader based on that
                return new CachingDataReaderCacheReader((DbQueryResults)value,
                    behavior);
            }
            else
            {                
                Interlocked.Increment(ref cacheMisses);
                return new EFCachingDataReaderCacheWriter(
                    this.WrappedCommand.ExecuteReader(behavior),
                    delegate(DbQueryResults entry)
                    {
                        try
                        {
                            item.Value = entry;
                            cache.PutItem(cacheKey, item, WrappedCommand);
                            Interlocked.Increment(ref cacheAdds);
                        }
                        catch (Exception exc)
                        {
                            Logger.Instance.TraceError(exc.Message);
                        }
                    },
                    behavior);
            }
        }
    }
}
