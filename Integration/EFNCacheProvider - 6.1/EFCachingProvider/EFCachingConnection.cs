// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Configuration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Integrations.EntityFramework.Toolkit;

namespace Alachisoft.NCache.Integrations.EntityFramework
{
    /// <summary>
    /// Implementation of <see cref="DbConnection"/> with support for caching of Entity Framework queries.
    /// </summary>
    public class EFCachingConnection : DbConnectionWrapper
    {
        /// <summary>
        /// Initializes a new instance of the EFCachingConnection class.
        /// </summary>
        public EFCachingConnection()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the EFCachingConnection class.
        /// </summary>
        /// <param name="wrappedConnection">The wrapped connection.</param>
        public EFCachingConnection(DbConnection wrappedConnection)
        {
            this.WrappedConnection = wrappedConnection;
        }
       
        /// <summary>
        /// Gets the name of the default wrapped provider.
        /// </summary>
        /// <returns>Name of the default wrapped provider.</returns>
        protected override string DefaultWrappedProviderName
        {
            get { return EFCachingProviderConfiguration.DefaultWrappedProvider; }
        }

        ///// <summary>
        ///// Gets the <see cref="T:System.Data.Common.DbProviderFactory"/> for this <see cref="T:System.Data.Common.DbConnection"/>.
        ///// </summary>
        ///// <value></value>
        ///// <returns>
        ///// A <see cref="T:System.Data.Common.DbProviderFactory"/>.
        ///// </returns>
        //protected override DbProviderFactory DbProviderFactory
        //{
        //    get { return EFCachingProviderFactory.Instance; }
        //}

        /// <summary>
        /// Starts a database transaction.
        /// </summary>
        /// <param name="isolationLevel">Specifies the isolation level for the transaction.</param>
        /// <returns>
        /// An object representing the new transaction.
        /// </returns>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new EFCachingTransaction(WrappedConnection.BeginTransaction(isolationLevel), this);
        }
    }
}
