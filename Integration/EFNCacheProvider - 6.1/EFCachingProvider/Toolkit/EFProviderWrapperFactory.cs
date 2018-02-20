// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Data.Common;

namespace Alachisoft.NCache.Integrations.EntityFramework.Toolkit
{
    /// <summary>
    /// Implementation of <see cref="DbProviderFactory"/> for EFProviderWrapper.
    /// </summary>
    public class EFProviderWrapperFactory : DbProviderFactoryBase
    {
        /// <summary>
        /// Gets the singleton instance of the EFProviderWrapper factory.
        /// </summary>
        /// <value>The singleton instance.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "Class is immutable")]
        public static readonly EFProviderWrapperFactory Instance = new EFProviderWrapperFactory();

        /// <summary>
        /// Prevents a default instance of the EFProviderWrapperFactory class from being created.
        /// </summary>
        private EFProviderWrapperFactory()
            : base(EFProviderWrapperServices.Instance)
        {
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbConnection"/> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbConnection"/>.
        /// </returns>
        public override System.Data.Common.DbConnection CreateConnection()
        {
            return new EFProviderWrapperConnection();
        }
    }
}
