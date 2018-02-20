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
using System.Configuration;

using Alachisoft.NCache.Common.Configuration;

namespace Alachisoft.NCache.Integrations.EntityFramework.Config
{
    /// <summary>
    /// Represent caching policy
    /// </summary>
    public class CachePolicy
    {   
        /// <summary>
        /// Type of expiration
        /// </summary>
        public enum Expirations
        {
            /// <summary>
            /// Item expires until specified interval
            /// </summary>
            Absolute,

            /// <summary>
            /// Item expires if not used for specified interval
            /// </summary>
            Sliding
        }        

        private Expirations _expType;
       
        /// <summary>
        /// Avoid caching
        /// </summary>
        public static readonly CachePolicy CacheNone;

        /// <summary>
        /// Static constructor
        /// </summary>
        static CachePolicy()
        {
            ///Default policy with caching disabled
            CacheNone = new CachePolicy()
            {
                DbSyncDependency = true,                 
                Enabled = false,
                ExpirationTime = 180
            };
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        internal CachePolicy()
        {
            ///Initialize to default values
            this.ExpirationType = Expirations.Absolute;
            this.ExpirationTime = 180;
            this.Enabled = true;
            this.DbSyncDependency = true;
        }

        /// <summary>
        /// Get or set the expiration type
        /// </summary>
        public Expirations ExpirationType
        {
            get { return this._expType; }
            set { this._expType = value; }
        }

        /// <summary>
        /// Get or set expiration type's string value
        /// </summary>
        [ConfigurationAttribute("expiration-type")]
        public string TypeString
        {
            get
            {
                return this._expType.ToString();
            }
            set
            {
                this._expType = Expirations.Absolute;
                if (value != null)
                {
                    switch (value.ToLower())
                    {
                        case "absolute":
                        case "":
                            this._expType = Expirations.Absolute;
                            break;
                        case "sliding":
                            this._expType = Expirations.Sliding;
                            break;
                        default:
                            throw new ConfigurationErrorsException("Unknown \"expiration-type\" specified.");
                    }
                }
            }
        }        

         ///<summary>
        ///Get or set whether caching is enabled or not. For CacheAll policy it always returns true
         ///</summary>
        [ConfigurationAttribute("enabled")]
        public virtual bool Enabled { get; set; }

        /// <summary>
        /// Get or set expiration time in seconds
        /// </summary>
        [ConfigurationAttribute("expiration-time","sec")]
        public int ExpirationTime { get; set; }

        /// <summary>
        /// Get or set whether database synchronization is enabled or not
        /// </summary>
        [ConfigurationAttribute("dbsyncdependency")]
        public bool DbSyncDependency { get; set; }      
    }
}
