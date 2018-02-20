// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees;

using Alachisoft.NCache.Integrations.EntityFramework.Toolkit;
using Alachisoft.NCache.Integrations.EntityFramework;
using System.Data.Entity.Core.Common;
using Alachisoft.NCache.Integrations.EntityFramework.Util;
using Alachisoft.NCache.Integrations.EntityFramework.Config;
using Alachisoft.NCache.Integrations.EntityFramework.Caching;
using System.Collections.Generic;
using System;

namespace Alachisoft.NCache.Integrations.EntityFramework
{
    /// <summary>
    /// Implementation of <see cref="DbProviderServices"/> for EFCachingProvider.
    /// </summary>
    public class EFCachingProviderServices : DbProviderServicesBase, IDisposable
    {
        /// <summary>
        /// Prevents a default instance of the EFCachingProviderServices class from being created.
        /// </summary>
        internal EFCachingProviderServices()
        {
        }

        /// <summary>
        /// Gets the singleton instance of <see cref="EFCachingProviderServices"/>.
        /// </summary>
        /// <value>The singleton instance.</value>
        public static EFCachingProviderServices Instance = new EFCachingProviderServices();

        /// <summary>
        /// Gets the default name of the wrapped provider.
        /// </summary>
        /// <returns>
        /// Default name of the wrapped provider (to be used when
        /// provider is not specified in the connction string)
        /// </returns>
        protected override string DefaultWrappedProviderName
        {
            get { return EFCachingProviderConfiguration.DefaultWrappedProvider; }
        }

        /// <summary>
        /// Gets the provider invariant iname.
        /// </summary>
        /// <returns>Provider invariant name.</returns>
        protected override string ProviderInvariantName
        {
            get { return "EFCachingProvider"; }
        }

        /// <summary>
        /// Creates the command definition wrapper.
        /// </summary>
        /// <param name="wrappedCommandDefinition">The wrapped command definition.</param>
        /// <param name="commandTree">The command tree.</param>
        /// <returns>
        /// The <see cref="DbCommandDefinitionWrapper"/> object.
        /// </returns>
        public override DbCommandDefinitionWrapper CreateCommandDefinitionWrapper(DbCommandDefinition wrappedCommandDefinition, System.Data.Entity.Core.Common.CommandTrees.DbCommandTree commandTree)
        {
            return new EFCachingCommandDefinition(wrappedCommandDefinition, commandTree);
        }

        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest dbProviderManifest, System.Data.Entity.Core.Common.CommandTrees.DbCommandTree dbCommandTree)
        {
            DbCommandDefinition dbCommandDefinition = base.CreateCommandDefinition(dbProviderManifest, dbCommandTree);
            return new EFCachingCommandDefinition(dbCommandDefinition, dbCommandTree);
        }

        protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            return base.GetProviderManifest(manifestToken);
        }

        protected override string GetDbProviderManifestToken(System.Data.Common.DbConnection connection)
        {
            return base.GetProviderManifestToken(connection);
        }

        protected override void DbCreateDatabase(System.Data.Common.DbConnection connection, int? commandTimeout, System.Data.Entity.Core.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            base.CreateDatabase(connection, commandTimeout, storeItemCollection);
        }

        protected override string DbCreateDatabaseScript(string providerManifestToken, System.Data.Entity.Core.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            return base.CreateDatabaseScript(providerManifestToken, storeItemCollection);
        }

        protected override bool DbDatabaseExists(System.Data.Common.DbConnection connection, int? commandTimeout, System.Data.Entity.Core.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            return base.DatabaseExists(connection, commandTimeout, storeItemCollection);
        }

        protected override void DbDeleteDatabase(System.Data.Common.DbConnection connection, int? commandTimeout, System.Data.Entity.Core.Metadata.Edm.StoreItemCollection storeItemCollection)
        {
            base.DeleteDatabase(connection, commandTimeout, storeItemCollection);
        }

        protected override void SetDbParameterValue(System.Data.Common.DbParameter parameter, System.Data.Entity.Core.Metadata.Edm.TypeUsage parameterType, object value)
        {
            base.SetDbParameterValue(parameter, parameterType, value);
        }

        protected override System.Data.Entity.Spatial.DbSpatialDataReader GetDbSpatialDataReader(System.Data.Common.DbDataReader fromReader, string manifestToken)
        {
            return base.GetSpatialDataReader(fromReader, manifestToken);
        }

        protected override System.Data.Entity.Spatial.DbSpatialServices DbGetSpatialServices(string manifestToken)
        {
            return base.GetSpatialServices(manifestToken);
        }

        public override object GetService(System.Type serviceType, object key)
        {
            {
                return base.GetService(serviceType, key);
            }
        }

        public override System.Collections.Generic.IEnumerable<object> GetServices(System.Type type, object key)
        {
            List<object> services = new List<object>();
            services.Add(new EFCommandInterceptor());
            return services;
        }

        public override void RegisterInfoMessageHandler(System.Data.Common.DbConnection connection, System.Action<string> handler)
        {
            base.RegisterInfoMessageHandler(connection, handler);
        }

        public override DbCommandDefinition CreateCommandDefinition(System.Data.Common.DbCommand prototype)
        {
            var cachingCommand = prototype as DbCommandWrapperExtended;

            var commandDefinition = base.CreateCommandDefinition(cachingCommand != null ? cachingCommand.WrappedCommand : prototype);

            return cachingCommand != null ? new EFCachingCommandDefinition(commandDefinition, cachingCommand.CommandTree) : commandDefinition;
        }

        public void Dispose()
        {
            Application.Instance.Dispose();
        }
    }
}

