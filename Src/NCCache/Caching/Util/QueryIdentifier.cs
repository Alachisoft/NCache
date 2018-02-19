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

namespace Alachisoft.NCache.Caching.Util
{
    class QueryIdentifier : IComparable
    {
        private string _query = string.Empty;
        private ulong _refCount = 0;

        public string Query
        {
            get { return _query; }
            set { _query = value; }
        }

        public ulong ReferenceCount
        {
            get { return _refCount; }
            set { _refCount = value; }
        }

        public QueryIdentifier(string query)
        {
            _query = query;
            _refCount = 1;
        }

        public void AddRef()
        {
            lock (this)
            {
                _refCount++;
            }
        }

        public override int GetHashCode()
        {
            return _query.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else
            {
                QueryIdentifier other = obj as QueryIdentifier;
                if (other == null)
                {
                    return _query.Equals(obj.ToString());
                }
                else
                {
                    return this.Query.Equals(other.Query);
                }
            }
        }

        public override string ToString()
        {
            return _query;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            int result = 0;
            if (obj != null && obj is QueryIdentifier)
            {
                QueryIdentifier other = (QueryIdentifier)obj;
                if (other._refCount > _refCount)
                    result = -1;
                else if (other._refCount < _refCount)
                    result = 1;
            }
            return result;
        }

        #endregion
    }
}
