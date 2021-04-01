//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System.Collections;

namespace Alachisoft.NCache.Web.SessionStateManagement
{
    /// <summary>Holds SessionState settings, that are read from config file.</summary>
    public class NCacheSessionStateSettings
    {
        private Hashtable _primaryCache = new Hashtable();
        private Hashtable _secondaryCaches = new Hashtable();

        /// <summary>
        /// recycle interval in minutes.
        /// after this interval the secondary cache connection is recycled;
        /// the default interval is 10 minutes.
        /// user can provide -1 if does not want to recycle the connection.
        /// </summary>
        private int _recycleInterval = 10; 

        /// <summary>Get or set the PrimaryCache table with the respective SId-Prefix</summary>
        public Hashtable PrimaryCache
        {
            get { return this._primaryCache; }
            set { this._primaryCache = value; }
        }

        /// <summary>Get or set the Hashtable of secondary CacheID's and their respective SID-Prefix(s).</summary>
        public Hashtable SecondaryCaches 
        {
            get { return this._secondaryCaches; }
            set { this._secondaryCaches = value; }
        }

        public int RecycleInterval
        {
            get { return this._recycleInterval; }
            set { this._recycleInterval = value; }
        }
    }
}