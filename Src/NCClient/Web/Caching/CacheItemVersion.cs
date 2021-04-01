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

using System;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Represents the version of each cache item. An instance of this class is used 
    /// in the optimistic concurrency model to ensure the data integrity.
    /// </summary>
    public class CacheItemVersion : IComparable
    {
        private ulong _version;

        internal CacheItemVersion() { }
        internal CacheItemVersion(ulong version)
        {
            _version = version;
        }

        /// <summary>
        /// Gets and sets the version.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Version
        {
            get { return _version; }
            set { _version = value; }
        }

        #region IComparable Members
        /// <summary>
        /// Compares an object with this instance of CacheItemVersion.
        /// </summary>
        /// <param name="obj">An object to compare with this instance of CacheItemVersion</param>
        /// <returns>0 if two instances are equal. An integer greater than 0 if this instance is greater.
        /// An integer less than 0 if this instance is smaller.</returns>
        public int CompareTo(object obj)
        {
            if (obj is CacheItemVersion)
            {
                return ((CacheItemVersion)obj).Version.CompareTo(this.Version);
            }
            return -1;
        }

        #endregion
        /// <summary>
        /// Tells if two instances of this class are equal.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>true if two instances of this class are equal.</returns>
        public override bool Equals(object obj)
        {
            return this.CompareTo(obj) == 0;
        }

        /// <summary>
        /// The string representation of this class.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Version.ToString();
        }
    }
}
