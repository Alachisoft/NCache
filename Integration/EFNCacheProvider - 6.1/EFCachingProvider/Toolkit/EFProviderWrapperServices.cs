// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Data.Entity.Core.Common;

namespace Alachisoft.NCache.Integrations.EntityFramework.Toolkit
{
    /// <summary>
    /// <see cref="DbProviderServices" /> implementation for EFProviderWrapper
    /// </summary>
    internal class EFProviderWrapperServices : DbProviderServicesBase
    {
        /// <summary>
        /// Initializes static members of the EFProviderWrapperServices class.
        /// </summary>
        static EFProviderWrapperServices()
        {
            Instance = new EFProviderWrapperServices();
        }

        /// <summary>
        /// Gets the singleton instance of <see cref="EFProviderWrapperServices"/>.
        /// </summary>
        /// <value>The singleton instance.</value>
        internal static EFProviderWrapperServices Instance { get; private set; }

        /// <summary>
        /// Gets the name default of the wrapped provider.
        /// </summary>
        /// <returns>Name of the default wrapped provider.</returns>
        protected override string DefaultWrappedProviderName
        {
            get { throw new NotSupportedException("Default wrapped provider is not supported"); }
        }

        /// <summary>
        /// Gets the provider invariant name.
        /// </summary>
        /// <returns>Provider invariant name: 'EFProviderWrapper'</returns>
        protected override string ProviderInvariantName
        {
            get { return "EFProviderWrapper"; } 
        }
    }
}
