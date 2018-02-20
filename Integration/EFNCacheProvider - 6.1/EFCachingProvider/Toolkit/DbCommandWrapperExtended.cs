// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Globalization;

using Alachisoft.NCache.Integrations.EntityFramework.Util;
using Alachisoft.NCache.Integrations.EntityFramework.Caching;
using System.Data.Entity.Core.Common;
using System.Data.Common;

namespace Alachisoft.NCache.Integrations.EntityFramework.Toolkit
{
    public class DbCommandWrapperExtended : DbCommandWrapper
    {
        private DbTransaction transaction;

        /// <summary>
        /// Initializes a new instance of the DbCommandWrapperExtended class.
        /// </summary>
        /// <param name="wrappedCommand">The wrapped command.</param>
        /// <param name="definition">The command definition.</param>
        public DbCommandWrapperExtended(System.Data.Common.DbCommand wrappedCommand, EFCachingCommandDefinition definition)
            : base(wrappedCommand, definition)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="P:System.Data.Common.DbCommand.DbTransaction"/> within which this <see cref="T:System.Data.Common.DbCommand"/> object executes.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The transaction within which a Command object of a .NET Framework data provider executes. The default value is a null reference (Nothing in Visual Basic).
        /// </returns>
        protected override System.Data.Common.DbTransaction DbTransaction
        {
            get
            {
                return this.transaction;
            }

            set
            {
                transaction = value as EFCachingTransaction;
                if (this.transaction != null)
                {
                    WrappedCommand.Transaction = ((EFCachingTransaction)transaction).WrappedTransaction;
                }
                else
                {
                    WrappedCommand.Transaction = value;
                }
            }
        }

        /// <summary>
        /// Gets <see cref="EFCachingConnection"/> used by this <see cref="T:System.Data.Common.DbCommand"/>.
        /// </summary>
        /// <returns>
        /// The connection to the data source.
        /// </returns>
        protected new System.Data.Common.DbConnection Connection
        {
            get { return base.Connection; }
        }

        protected new EFCachingCommandDefinition Definition
        {
            get { return base.Definition; }
        }

        /// <summary>
        /// Get cache key from command text
        /// </summary>
        /// <returns></returns>
        protected string GetCacheKey()
        {
            if (this.CommandText == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder(this.CommandText.StripTabsAndNewlines());

            string cmdString = WrappedCommand.ToString();

            foreach (System.Data.Common.DbParameter parameter in Parameters)
            {
                if (parameter.Direction != ParameterDirection.Input)
                {
                    // we only cache quries with input parameters
                    return null;
                }
                sb = sb.Replace("@" + parameter.ParameterName, GetLiteralValue(parameter.Value));
            }

            return sb.ToString();
        }
        protected string GetStoredProcedureKey()
        {
            if (this.CommandText == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder(this.CommandText.StripTabsAndNewlines());
            sb.Append("(");

            foreach (System.Data.Common.DbParameter parameter in Parameters)
            {
                sb.AppendFormat("{0} {1},", parameter.ParameterName, GetLiteralValue(parameter.Value));                
            }
            sb.Replace(',', ')', sb.Length - 1, 1);

            return sb.ToString();
        }

        private static string GetLiteralValue(object value)
        {
            if (value is string)
            {
                return "'" + value.ToString().Replace("'", "''") + "'";
            }
            else
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }
    }
}
