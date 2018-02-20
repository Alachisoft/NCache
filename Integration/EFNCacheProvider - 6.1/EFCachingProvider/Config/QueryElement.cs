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

using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Integrations.EntityFramework.Util;

namespace Alachisoft.NCache.Integrations.EntityFramework.Config
{
    /// <summary>
    /// Represent "query" element in "custom-policy" section
    /// </summary>
    public sealed class QueryElement : ICloneable
    {
        private string _queryText;
        //private string _cacheableParameterString;
        //private List<string> _parameterList;

        /// <summary>
        /// Get or set query text
        /// </summary>
        [ConfigurationAttribute("query-text")]
        public string QueryText
        {
            get { return this._queryText; }
            set { this._queryText = (value != null) ? value.StripTabsAndNewlines() : string.Empty; }
        }

        /// <summary>
        /// Gets/Sets the cache able parameters separated by ','.
        /// For e.g. param_1,param_2
        /// </summary>
        //[ConfigurationAttribute("vary-by-cache-param")]
        //public string CacheableParameterString
        //{
        //    get { return _cacheableParameterString; }
        //    set { _cacheableParameterString = value; }
        //}

        //public List<string> CacheableParameterList
        //{
        //    get
        //    {
        //        if (_parameterList == null)
        //        {
        //            _parameterList = new List<string>();

        //            if (!string.IsNullOrEmpty(_cacheableParameterString))
        //            {
        //                string[] paramStr = _cacheableParameterString.Split(new char[]{','});

        //                foreach (string param in paramStr)
        //                {
        //                    if(!string.IsNullOrEmpty(param))
        //                        _parameterList.Add(param);
        //                }
        //            }
        //        }
        //        return _parameterList;
        //    }
        //}

        /// <summary>
        /// Get or set comments on query element
        /// </summary>
        [ConfigurationComment()]
        public string Comment { get; set; }

        [ConfigurationComment()]
        public string CommentQuery { get; set; }
        #region ICloneable Members

        public object Clone()
        {
            return new QueryElement()
            {
             //  ExpirationType = base.ExpirationType,
             //  ExpirationTime = base.ExpirationTime,
             //   DbSyncDependency = base.DbSyncDependency,
             //   Enabled = base.Enabled,
                QueryText = this.QueryText,
            };
        }

        #endregion
    }
}
