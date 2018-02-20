// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Configuration;
using System.IO;

using Alachisoft.NCache.Integrations.EntityFramework.Caching;

namespace Alachisoft.NCache.Integrations.EntityFramework
{
    /// <summary>
    /// Default configuration settings for EFCachingProvider.
    /// </summary>
    public static class EFCachingProviderConfiguration
    {
        /// <summary>
        /// Initializes static members of the EFCachingProviderConfiguration class.
        /// </summary>
        static EFCachingProviderConfiguration()
        {
            DefaultWrappedProvider = ConfigurationSettings.AppSettings["EFCachingProvider.wrappedProvider"];
        }

        /// <summary>
        /// Gets or sets the default wrapped provider.
        /// </summary>
        /// <value>The default wrapped provider.</value>
        public static string DefaultWrappedProvider { get; set; }

        /// <summary>
        /// Gets or sets default caching <see cref="ICache"/> implementation which should be used for new connections.
        /// </summary>
        public static ICache DefaultCache { get; set; }

        /// <summary>
        /// Gets or sets default caching policy to be applied to all new connections.
        /// </summary>
        //public static CachingPolicy DefaultCachingPolicy { get; set; }
    }
}
