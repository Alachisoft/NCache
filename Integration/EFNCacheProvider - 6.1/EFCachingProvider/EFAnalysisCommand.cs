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
using System.Data.Common;
using System.Data;

using Alachisoft.NCache.Integrations.EntityFramework.Util;
using Alachisoft.NCache.Integrations.EntityFramework.Toolkit;
using Alachisoft.NCache.Integrations.EntityFramework.Analysis;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Toolkit;
using System.Data.Entity.Core.Common.CommandTrees;
using Alachisoft.NCache.Integrations.EntityFramework.Caching;

namespace Alachisoft.NCache.Integrations.EntityFramework
{
    /// <summary>
    /// Implementation of <see cref="DbCommand"/> wrapper which implements query analysis.
    /// </summary>
    public sealed class EFAnalysisCommand : DbCommandWrapperExtended
    {
        /// <summary>
        /// Initializes a new instance of the EFAnalysisCommand class.
        /// </summary>
        /// <param name="wrappedCommand">The wrapped command.</param>
        /// <param name="commandDefinition">The command definition.</param>
        public EFAnalysisCommand(DbCommand wrappedCommand, EFCachingCommandDefinition commandDefinition)
            : base(wrappedCommand, commandDefinition)
        {
        }

        /// <summary>
        /// Executes the command text against the connection.
        /// </summary>
        /// <param name="behavior">An instance of <see cref="T:System.Data.CommandBehavior"/>.</param>
        /// <returns>
        /// A <see cref="T:System.Data.Common.DbDataReader"/>.
        /// </returns>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            if (!this.Definition.IsModification)
            {
                Query query = Query.CreateQuery(this.WrappedCommand, this.Definition.IsStoredProcedure);

                if (query != null)
                {
                    AnalysisManager.Instance.AnalyzeQuery(query);
                }
            }

            return WrappedCommand.ExecuteReader(behavior);
        }
    }
}
