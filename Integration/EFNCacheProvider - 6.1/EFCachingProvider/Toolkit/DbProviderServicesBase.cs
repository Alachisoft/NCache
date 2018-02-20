// Copyright (c) Microsoft Corporation.  All rights reserved.

using Alachisoft.NCache.Integrations.EntityFramework.Caching;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Common;
using System.Reflection;

namespace Alachisoft.NCache.Integrations.EntityFramework.Toolkit
{
    /// <summary>
    /// Common implementation of <see cref="DbProviderServices"/> methods.
    /// </summary>
    public abstract class DbProviderServicesBase : System.Data.Entity.Core.Common.DbProviderServices
    {
        private Dictionary<string, Func<System.Data.Entity.Core.Common.DbProviderManifest, System.Data.Entity.Core.Common.CommandTrees.DbCommandTree, System.Data.Entity.Core.Common.DbCommandDefinition>> createDbCommandDefinitionFunctions = new Dictionary<string, Func<System.Data.Entity.Core.Common.DbProviderManifest, System.Data.Entity.Core.Common.CommandTrees.DbCommandTree, System.Data.Entity.Core.Common.DbCommandDefinition>>();

        /// <summary>
        /// Gets the provider invariant iname.
        /// </summary>
        /// <returns>Provider invariant name.</returns>
        protected abstract string ProviderInvariantName { get; }

        /// <summary>
        /// Gets the default name of the wrapped provider.
        /// </summary>
        /// <returns>Default name of the wrapped provider (to be used when 
        /// provider is not specified in the connction string)</returns>
        protected abstract string DefaultWrappedProviderName { get; }

        /// <summary>
        /// Creates the command definition wrapper.
        /// </summary>
        /// <param name="wrappedCommandDefinition">The wrapped command definition.</param>
        /// <param name="commandTree">The command tree.</param>
        /// <returns><see cref="DbCommandDefinitionWrapper"/> object.</returns>
        public virtual DbCommandDefinitionWrapper CreateCommandDefinitionWrapper(System.Data.Entity.Core.Common.DbCommandDefinition wrappedCommandDefinition, System.Data.Entity.Core.Common.CommandTrees.DbCommandTree commandTree)
        {
            return new DbCommandDefinitionWrapper(wrappedCommandDefinition, commandTree, (cmd, def) => EFDbCommandFactory.GetCommandWrapper(cmd, def));
        }

        /// <summary>
        /// Creates the provider manifest wrapper.
        /// </summary>
        /// <param name="providerInvariantName">Provider invariant name.</param>
        /// <param name="providerManifest">The provider manifest to be wrapped.</param>
        /// <returns><see cref="DbProviderManifest"/> wrapper for given provider invariant name wrapping given provider manifest.</returns>
        public virtual System.Data.Entity.Core.Common.DbProviderManifest CreateProviderManifest(string providerInvariantName, System.Data.Entity.Core.Common.DbProviderManifest providerManifest)
        {
            return new DbProviderManifestWrapper(
                this.ProviderInvariantName,
                providerInvariantName,
                providerManifest);
        }

        /// <summary>
        /// Gets the provider manifest token.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <returns>Provider Manfiest Token suitable for inclusion in SSDL file and connection string</returns>
        /// <remarks>
        /// The provider manifest token is created by concatenating wrapped provider invariant name and its 
        /// token separated by semicolon, for example when wrapping SqlClient for SQL Server 2005 the provider 
        /// manifest token will be "System.Data.SqlClient;2005"
        /// </remarks>
        protected override string GetDbProviderManifestToken(DbConnection connection)
        {
            DbConnectionWrapper wrapper = (DbConnectionWrapper)connection;
            DbConnection wrappedConnection = wrapper.WrappedConnection;
            System.Data.Entity.Core.Common.DbProviderServices services = System.Data.Entity.Core.Common.DbProviderServices.GetProviderServices(wrappedConnection);

            string token = wrapper.WrappedProviderInvariantName + ";" + services.GetProviderManifestToken(wrappedConnection);
            return token;
        }

        /// <summary>
        /// Gets the provider manifest for given provider manifest token.
        /// </summary>
        /// <param name="manifestToken">The provider manifest token.</param>
        /// <returns><see cref="DbProviderManifest"/> for a given token.</returns>
        protected override System.Data.Entity.Core.Common.DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            string providerInvariantName;
            string realToken;

            // check if the name of the wrapped provider is specified in the token
            int p = manifestToken.IndexOf(';');
            if (p < 0)
            {
                // wrapped provider is not in the token - use default one
                realToken = manifestToken;
                providerInvariantName = this.DefaultWrappedProviderName;
            }
            else
            {
                // extract provider name from the token
                providerInvariantName = manifestToken.Substring(0, p);
                realToken = manifestToken.Substring(p + 1);
            }

            // retrieve wrapped provider manifest
            System.Data.Entity.Core.Common.DbProviderServices services = GetProviderServicesByName(providerInvariantName);
            System.Data.Entity.Core.Common.DbProviderManifest wrappedProviderManifest = services.GetProviderManifest(realToken);
            System.Data.Entity.Core.Common.DbProviderManifest wrapperManifest = this.CreateProviderManifest(providerInvariantName, wrappedProviderManifest);

            return wrapperManifest;
        }

        /// <summary>
        /// Creates the command definition wrapper for a given provider manifest and command tree.
        /// </summary>
        /// <param name="providerManifest">The provider manifest.</param>
        /// <param name="commandTree">The command tree.</param>
        /// <returns><see cref="DbCommandDefinition"/> object.</returns>
        protected override System.Data.Entity.Core.Common.DbCommandDefinition CreateDbCommandDefinition(System.Data.Entity.Core.Common.DbProviderManifest providerManifest, System.Data.Entity.Core.Common.CommandTrees.DbCommandTree commandTree)
        {
            var wrapperManifest = (DbProviderManifestWrapper)providerManifest;
            var createDbCommandDefinitionFunction = this.GetCreateDbCommandDefinitionFunction(wrapperManifest.WrappedProviderManifestInvariantName);

            System.Data.Entity.Core.Common.DbCommandDefinition definition = createDbCommandDefinitionFunction(wrapperManifest.WrappedProviderManifest, commandTree);
            return this.CreateCommandDefinitionWrapper(definition, commandTree);
        }

        /// <summary>
        /// Gets provider services object given provider invariant name.
        /// </summary>
        /// <param name="providerInvariantName">Provider invariant name.</param>
        /// <returns><see cref="DbProviderServices"/> object for a given invariant name.</returns>
        private static System.Data.Entity.Core.Common.DbProviderServices GetProviderServicesByName(string providerInvariantName)
        {
            DbProviderFactory factory = DbProviderFactories.GetFactory(providerInvariantName);
            if (factory == null)
            {
                throw new ArgumentException("Invalid provider factory: " + providerInvariantName);
            }

            IServiceProvider serviceProvider = factory as IServiceProvider;
            if (serviceProvider == null)
            {
                throw new ArgumentException("Provider does not support Entity Framework - IServiceProvider is not supported");
            }

            System.Data.Entity.Core.Common.DbProviderServices providerServices = (System.Data.Entity.Core.Common.DbProviderServices)serviceProvider.GetService(typeof(System.Data.Entity.Core.Common.DbProviderServices));
            if (providerServices == null)
            {
                throw new ArgumentException("Provider does not support Entity Framework - DbProviderServices is not supported");
            }

            return providerServices;
        }

        private Func<System.Data.Entity.Core.Common.DbProviderManifest, System.Data.Entity.Core.Common.CommandTrees.DbCommandTree, System.Data.Entity.Core.Common.DbCommandDefinition> GetCreateDbCommandDefinitionFunction(string providerInvariantName)
        {
            Func<System.Data.Entity.Core.Common.DbProviderManifest, System.Data.Entity.Core.Common.CommandTrees.DbCommandTree, System.Data.Entity.Core.Common.DbCommandDefinition> result;
            lock (this.createDbCommandDefinitionFunctions)
            {
                if (!this.createDbCommandDefinitionFunctions.TryGetValue(providerInvariantName, out result))
                {
                    System.Data.Entity.Core.Common.DbProviderServices ps = GetProviderServicesByName(providerInvariantName);

                    // create a delegate to call CreateDbCommandDefinition using reflection
                    // (method is protected, but part of public API)
                    MethodInfo createCommandDefinitionMethodInfo = ps.GetType().GetMethod("CreateDbCommandDefinition", BindingFlags.NonPublic | BindingFlags.Instance);
                    result = (Func<System.Data.Entity.Core.Common.DbProviderManifest, System.Data.Entity.Core.Common.CommandTrees.DbCommandTree, System.Data.Entity.Core.Common.DbCommandDefinition>)Delegate.CreateDelegate(
                        typeof(Func<System.Data.Entity.Core.Common.DbProviderManifest, System.Data.Entity.Core.Common.CommandTrees.DbCommandTree, System.Data.Entity.Core.Common.DbCommandDefinition>),
                        ps,
                        createCommandDefinitionMethodInfo);
                    this.createDbCommandDefinitionFunctions[providerInvariantName] = result;
                }
            }

            return result;
        }
    }
}
