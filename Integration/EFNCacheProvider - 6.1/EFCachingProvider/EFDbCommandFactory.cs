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
using Alachisoft.NCache.Integrations.EntityFramework.Toolkit;
using Alachisoft.NCache.Integrations.EntityFramework.Config;
using Alachisoft.NCache.Integrations.EntityFramework.Caching;

namespace Alachisoft.NCache.Integrations.EntityFramework
{

    /// <summary>
    /// Provides method to initialize DbCommand according to policy
    /// </summary>
    internal static class EFDbCommandFactory
    {
        /// <summary>
        /// Get the DbCommand instance from the policies defined
        /// </summary>
        /// <param name="wrappedCommand"></param>
        /// <param name="commandDefinition"></param>
        /// <returns></returns>
        public static DbCommandWrapper GetCommandWrapper(DbCommand wrappedCommand, DbCommandDefinitionWrapper commandDefinition)
        {
            Application.ApplicationMode mode = Application.Instance.Mode;
            EFCachingCommandDefinition efCommandDefinition = new EFCachingCommandDefinition(commandDefinition, commandDefinition.CommandTree);
            switch (mode)
            {
                case Application.ApplicationMode.Analysis:
                    return new EFAnalysisCommand(wrappedCommand, efCommandDefinition);

                case Application.ApplicationMode.Cache:
                    return new EFCachingCommand(wrappedCommand, efCommandDefinition);

                default:
                    return new DbCommandWrapper(wrappedCommand, efCommandDefinition);
            }
        }
    }
}
