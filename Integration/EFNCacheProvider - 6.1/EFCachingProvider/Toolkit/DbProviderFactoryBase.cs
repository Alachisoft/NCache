// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Data.Common;
using System.Data.Entity.Core.Common;

namespace Alachisoft.NCache.Integrations.EntityFramework.Toolkit
{
    /// <summary>
    /// Common implementation of the <see cref="DbProviderFactory"/> methods.
    /// </summary>
    public abstract class DbProviderFactoryBase : DbProviderFactory, IServiceProvider
    {
        private System.Data.Entity.Core.Common.DbProviderServices services;

        /// <summary>
        /// Initializes a new instance of the DbProviderFactoryBase class.
        /// </summary>
        /// <param name="providerServices">The provider services.</param>
        protected DbProviderFactoryBase(System.Data.Entity.Core.Common.DbProviderServices providerServices)
        {
            this.services = providerServices;
        }

        /// <summary>
        /// Specifies whether the specific <see cref="T:System.Data.Common.DbProviderFactory"/> supports the <see cref="T:System.Data.Common.DbDataSourceEnumerator"/> class.
        /// </summary>
        /// <value></value>
        /// <returns>true if the instance of the <see cref="T:System.Data.Common.DbProviderFactory"/> supports the <see cref="T:System.Data.Common.DbDataSourceEnumerator"/> class; otherwise false.
        /// </returns>
        public override bool CanCreateDataSourceEnumerator
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbCommandBuilder"/> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbCommandBuilder"/>.
        /// </returns>
        public override DbCommandBuilder CreateCommandBuilder()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbConnectionStringBuilder"/> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbConnectionStringBuilder"/>.
        /// </returns>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbDataAdapter"/> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbDataAdapter"/>.
        /// </returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbDataSourceEnumerator"/> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbDataSourceEnumerator"/>.
        /// </returns>
        public override DbDataSourceEnumerator CreateDataSourceEnumerator()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbParameter"/> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbParameter"/>.
        /// </returns>
        public override DbParameter CreateParameter()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbConnection"/> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbConnection"/>.
        /// </returns>
        public abstract override DbConnection CreateConnection();

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>
        /// A service object of type <paramref name="serviceType"/>.
        /// -or-
        /// null if there is no service object of type <paramref name="serviceType"/>.
        /// </returns>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(DbProviderServices))
            {
                return this.services;
            }
            else
            {
                return null;
            }
        }
    }
}
