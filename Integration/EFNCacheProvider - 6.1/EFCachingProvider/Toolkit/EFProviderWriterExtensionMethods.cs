// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;

namespace Alachisoft.NCache.Integrations.EntityFramework.Toolkit
{
    /// <summary>
    /// Extension methods for handing wrapped providers.
    /// </summary>
    public static class EFProviderWriterExtensionMethods
    {
        /// <summary>
        /// Gets the underlying wrapper connection from the <see cref="ObjectContext"/>.
        /// </summary>
        /// <typeparam name="TConnection">Connection type.</typeparam>
        /// <param name="context">The object context.</param>
        /// <returns>Wrapper connection of a given type.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Type parameter must be specified explicitly.")]
        public static TConnection UnwrapConnection<TConnection>(this ObjectContext context) 
            where TConnection : DbConnection
        {
            return context.Connection.UnwrapConnection<TConnection>();
        }

        /// <summary>
        /// Tries to get the underlying wrapper connection from the <see cref="ObjectContext"/>.
        /// </summary>
        /// <typeparam name="TConnection">Connection type.</typeparam>
        /// <param name="context">The object context.</param>
        /// <param name="result">The result connection.</param>
        /// <returns>A value of true if the given connection type was found in the provider chain, false otherwise.</returns>
        public static bool TryUnwrapConnection<TConnection>(this ObjectContext context, out TConnection result) where TConnection : DbConnection
        {
            return context.Connection.TryUnwrapConnection<TConnection>(out result);
        }

        /// <summary>
        /// Gets the underlying wrapper connection from the <see cref="DbConnection"/>.
        /// </summary>
        /// <typeparam name="TConnection">Connection type.</typeparam>
        /// <param name="connection">The connection.</param>
        /// <returns>Wrapper connection of a given type.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Type parameter must be specified explicitly.")]
        public static TConnection UnwrapConnection<TConnection>(this DbConnection connection) where TConnection : DbConnection
        {
            TConnection result;

            if (connection.TryUnwrapConnection(out result))
            {
                return result;
            }

            throw new InvalidOperationException("Wrapper provider of type " + typeof(TConnection).FullName + " was not found in the chain.");
        }

        /// <summary>
        /// Tries to get the underlying wrapper connection from the <see cref="DbConnection"/>.
        /// </summary>
        /// <typeparam name="TConnection">Connection type.</typeparam>
        /// <param name="connection">The connection.</param>
        /// <param name="result">The result connection.</param>
        /// <returns>A value of true if the given connection type was found in the provider chain, false otherwise.</returns>
        public static bool TryUnwrapConnection<TConnection>(this DbConnection connection, out TConnection result) where TConnection : DbConnection
        {
            EntityConnection ec = connection as EntityConnection;
            if (ec != null)
            {
                connection = ec.StoreConnection;
            }

            while (connection is DbConnectionWrapper)
            {
                if (connection is TConnection)
                {
                    result = (TConnection)connection;
                    return true;
                }

                connection = ((DbConnectionWrapper)connection).WrappedConnection;
            }

            result = null;
            return false;
        }
    }
}
