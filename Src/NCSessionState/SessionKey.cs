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

namespace Alachisoft.NCache.Web.SessionState
{
    /// <summary>
    /// Implements attribute based data transfer strategy b/w the session and ncache. 
    /// In this strategy individual session attributes are replicated. This might have 
    /// performance and synchronization problems.
    /// </summary>
    [Serializable()]
    internal class SessionKey : IComparable, Runtime.Serialization.ICompactSerializable
    {
        private string _sessionID;
        private string _key;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="key"></param>
        internal SessionKey(string sid, string key)
        {
            _sessionID = sid;
            _key = key;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="key"></param>
        internal SessionKey(string compositeKey)
        {
            string[] keys = compositeKey.Split('.');
            _sessionID = keys[0];
            _key = keys[1];
        }

        /// <summary>
        /// 
        /// </summary>
        public string SessionID
        {
            get { return _sessionID; }
            set { _sessionID = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        #region	/                 --- IComparable Members ---           /

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            SessionKey other = obj as SessionKey;
            if (other != null)
            {
                if (String.Compare(_sessionID, other._sessionID) == 0)
                    return String.Compare(_key, other._key);
            }
            return -1;
        }

        public static string CompositeKey(string sid, string key)
        {
            string compositeKey = sid + "." + key;
            return compositeKey;
        }

        #endregion

        #region	/                 --- Object overrides ---           /

        public override bool Equals(object obj)
        {
            return CompareTo(obj) == 0;
        }

        public override int GetHashCode()
        {
            if (_key != null) return Key.GetHashCode();
            return _sessionID.GetHashCode();
        }

        public override string ToString()
        {
            return (_key == null || _key == string.Empty) ? _sessionID : _sessionID + "." + _key;
        }

        #endregion

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _sessionID = (string)reader.ReadObject();
            _key = (string)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_sessionID);
            writer.WriteObject(_key);
        } 
        #endregion
    }
}