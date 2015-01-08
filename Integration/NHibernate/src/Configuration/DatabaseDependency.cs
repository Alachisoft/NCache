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
using Alachisoft.NCache.Common.Configuration;

namespace Alachisoft.NCache.Integrations.NHibernate.Cache.Configuration
{
    class DatabaseDependency
    {
        private string _entityName;
        private string _type;
        private string _sqlStatement;
        private string _cacheKeyFormat = "NHibernateNCache:[en]#[pk]";
        private char _compositeKeySeperator = '$';

        [ConfigurationAttribute("entity-name")]
        public string EntityName
        {
            get { return _entityName; }
            set { _entityName = value; }
        }

        [ConfigurationAttribute("type")]
        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        [ConfigurationAttribute("sql-statement")]
        public string SqlStatement
        {
            get { return _sqlStatement; }
            set { _sqlStatement = value; }
        }

        [ConfigurationAttribute("cache-key-format")]
        public string CacheKeyFormat
        {
            get { return _cacheKeyFormat; }
            set { _cacheKeyFormat = value; }
        }

        [ConfigurationAttribute("composite-key-seperator")]
        public char CompositeKeySeperator
        {
            get { return _compositeKeySeperator; }
            set { _compositeKeySeperator = value; }
        }
    }
}
