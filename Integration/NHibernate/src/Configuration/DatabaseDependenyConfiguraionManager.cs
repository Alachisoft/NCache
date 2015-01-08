// Copyright (c) 2015 Alachisoft
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
// ===============================================================================
// Alachisoft (R) NCache Integrations
// NCache Provider for NHibernate
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Integrations.NHibernate.Cache.Configuration;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Integrations.NHibernate.Cache.Configuration
{
    class DatabaseDependenyConfiguraionManager
    {
        Hashtable dependencyHT = new Hashtable();
        private string _defaultCacheKeyFormat = "NHibernateNCache:[en]#[pk]";

        public DatabaseDependenyConfiguraionManager(DatabaseDependencies dependencies)
        {
            if (dependencies != null && dependencies.Dependencies!=null)
            {
                foreach (DatabaseDependency dependency in dependencies.Dependencies)
                {
                    ValidateDependency(dependency);
                    dependencyHT.Add(dependency.EntityName, dependency);
                }
            }
        }

        public DatabaseDependency GetDependency(string entityName)
        {
            return dependencyHT[entityName] as DatabaseDependency;
        }

        private void ValidateDependency(DatabaseDependency dependency)
        {
            if (string.IsNullOrEmpty(dependency.EntityName))
                throw new ConfigurationException("entity-name cannot be empty in dependency.");
            if (dependencyHT.Contains(dependency.EntityName))
                throw new ConfigurationException("Multiple dependencies can not be added for same object type : "+ dependency.EntityName );
            if(string.IsNullOrEmpty(dependency.Type))
                throw new ConfigurationException("dependency type cannot be empty in dependency: " + dependency.EntityName);

            dependency.Type = dependency.Type.ToLower();
            if (dependency.Type != "sql" && dependency.Type != "oracle" && dependency.Type != "oledb")
                throw new ConfigurationException("Invalid dependency type.");
            if(dependency.Type!="oledb" && string.IsNullOrEmpty(dependency.SqlStatement))
                throw new ConfigurationException("sql-statement cannot be empty for sql/oracle dependency: " + dependency.EntityName);
            if (string.IsNullOrEmpty(dependency.CacheKeyFormat) || !dependency.CacheKeyFormat.Contains("[pk]"))
                throw new ConfigurationException("Invalid cache-key-format in dependency " + dependency.EntityName+". cache-key-format must include [pk] tag.");
        }

        public string GetCacheKeyFormat(string entityName)
        {
            if (string.IsNullOrEmpty(entityName) || !this.dependencyHT.Contains(entityName))
                return _defaultCacheKeyFormat;
            else
                return (dependencyHT[entityName] as DatabaseDependency).CacheKeyFormat;
        }

    }
}
