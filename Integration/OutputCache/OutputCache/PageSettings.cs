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
// limitations under the License

namespace Alachisoft.NCache.Web.NOutputCache
{
    /// <summary>
    /// Hold page settings, that are read from config file
    /// </summary>
    internal class PageSettings
    {
        private string[] _varyByParam;
        private string[] _varyByHeader;
        private string _varyByCustom = null;
        private string _name = null;
        private bool _varyByAllParams = false;
        private bool _varyByAllHeaders = false;
        private int _expire = 0;
        private bool _enabled = true;
        private bool _get = false;
        private bool _post = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public PageSettings()
        {
            this._varyByParam = new string[0];
            this._varyByHeader = new string[0];
        }

        /// <summary>
        /// Get VaryByParams of output caching
        /// </summary>
        public string[] VaryByParam
        {
            get { return this._varyByParam; }
        }

        /// <summary>
        /// Get VaryByHeaders of output caching
        /// </summary>
        public string[] VaryByHeader
        {
            get { return this._varyByHeader; }
        }

        /// <summary>
        /// Get or set value of custom variable
        /// </summary>
        public string VaryByCustom
        {
            get { return this._varyByCustom; }
            set { this._varyByCustom = value; }
        }

        /// <summary>
        /// Get or set flag indicating if cache is enabled for http get request
        /// </summary>
        public bool Get
        {
            get { return this._get; }
            set { this._get = value; }
        }

        /// <summary>
        /// Get or set flag indicating if cache is enabled for http post request
        /// </summary>
        public bool Post
        {
            get { return this._post; }
            set { this._post = value; }
        }

        /// <summary>
        /// Get or set value indicating if caching is enabled on this page
        /// </summary>
        public bool CachingEnabled
        {
            get { return this._enabled; }
            set { this._enabled = value; }
        }

        /// <summary>
        /// Get or set page name
        /// </summary>
        public string PageName
        {
            get { return this._name; }
            set { this._name = value; }
        }

        /// <summary>
        /// Get or set page expiration time in seconds
        /// </summary>
        public int ExpirationTime
        {
            get { return this._expire; }
            set { this._expire = value; }
        }
        
        /// <summary>
        /// Get a flag indicating if cached page vary by all params
        /// </summary>
        public bool VaryByAllParams
        {
            get { return this._varyByAllParams; }
        }

        /// <summary>
        /// Get a flag indicating if cached page vary by all headers
        /// </summary>
        public bool VaryByAllHeaders
        {
            get { return this._varyByAllHeaders; }
        }

        /// <summary>
        /// Parse the varyByParmas string from config
        /// </summary>
        /// <param name="varyByParams">varyByParams string from config</param>
        public void ParseVaryByParams(string varyByParams)
        {
            this._varyByAllParams = this.VaryByAll(varyByParams);
            this._varyByParam = this.VaryBy(varyByParams);
        }

        /// <summary>
        /// Parse the varyByHeaders string from config
        /// </summary>
        /// <param name="varyByHeaders">varyByHeaders string from config</param>
        public void ParseVaryByHeaders(string varyByHeaders)
        {
            this._varyByAllHeaders = this.VaryByAll(varyByHeaders);
            this._varyByHeader = this.VaryBy(varyByHeaders);
        }

        /// <summary>
        /// Parse varyBy
        /// </summary>
        /// <param name="varyBy">varyBy string</param>
        /// <returns>varyBy array</returns>
        private string[] VaryBy(string varyBy)
        {
            string[] varyByArray = new string[0];

            if (!NOutputCache.IsNullOrEmpty(varyBy))
            {
                varyByArray = varyBy.Trim().Split(',');

                ///Trim the result, incase there are spaces
                for (int i = 0; i < varyByArray.Length; i++)
                {
                    varyByArray[i] = varyByArray[i].Trim();
                }
            }

            return varyByArray;
        }

        /// <summary>
        /// Checks if varyByAll is set
        /// </summary>
        /// <param name="varyBy">varyBy string</param>
        /// <returns>true if varyByAll, false otherwise</returns>
        private bool VaryByAll(string varyBy)
        {
            if (!NOutputCache.IsNullOrEmpty(varyBy))
            {
                return (varyBy.Trim() == "*");                
            }
            return false;
        }
    }
}