// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Integrations.EntityFramework.Toolkit
{
    /// <summary>
    /// <see cref="DbConnection"/> implementation for EFProviderWrapper
    /// </summary>
    internal class EFProviderWrapperConnection : DbConnectionWrapper
    {
        /// <summary>
        /// Gets the <see cref="T:System.Data.Common.DbProviderFactory"/> for this <see cref="T:System.Data.Common.DbConnection"/>.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// A <see cref="T:System.Data.Common.DbProviderFactory"/>.
        /// </returns>
        protected override System.Data.Common.DbProviderFactory DbProviderFactory
        {
            get
            {
                return EFProviderWrapperFactory.Instance;
            }
        }

        /// <summary>
        /// Gets the default wrapped provider.
        /// </summary>
        /// <returns>Name of the default wrapped provider.</returns>
        protected override string DefaultWrappedProviderName
        {
            get { throw new NotImplementedException(); }
        }
    }
}
