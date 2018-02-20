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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    /// <summary>
    /// A notification based dependency. This dependency can be used only with Sql Server 2005.
    /// Sql Server 2005 (Yukon) itself notifies the client application if there are any changes
    /// in the data. Microsoft has implemented this dependency (SqlDependency) in Whidbey under
    /// System.Data.SqlClient namespace. This class uses the same SqlDependency class to create 
    /// the dependency object.
    /// </summary>
    [Serializable]
    public class SqlYukonCacheDependency : DependencyHint, ICompactSerializable
    {
        [NonSerialized]
        private CacheDbSyncManager              _cdbSyncManager = null;
        [NonSerialized]

        private NotificationBasedDependencyManager          _notifBasedDepManager = null;

        private string                              _conStr;
        private string                              _queryStr;
        private IDictionary _cmdParams;
        private CommandType                         _cmdType;

        public SqlYukonCacheDependency()
        {
            _hintType = ExpirationHintType.SqlYukonCacheDependency;
        }
        /// <summary>
        /// Creates a SqlYukonCacheDependency object based on a SqlCommand.
        /// </summary>
        /// <param name="conStr"></param>
        /// <param name="queryStr"></param>

        public SqlYukonCacheDependency(string conStr, string queryStr)
            : this(conStr, queryStr, CommandType.Text, new Dictionary<string,SqlCmdParams>())
        { }

        public SqlYukonCacheDependency(string conStr, string queryStr, CommandType cmdType, IDictionary cmdParams)
        {
            _hintType = ExpirationHintType.SqlYukonCacheDependency;

            if (conStr == null || queryStr == null)
            {
                throw new ArgumentNullException("connection or query string is null");
            }

            _conStr = conStr;
            _queryStr = queryStr;
            _cmdType = cmdType;
            _cmdParams = cmdParams;
        }

        public string ConnectionString
        {
            get { return this._conStr; }
        }

        internal CommandType CommandType
        {
            get { return this._cmdType; }
        }

        public string QueryString
        {
            get { return this._queryStr; }
        }

        internal IDictionary CommandParams
        {
            get { return this._cmdParams; }
        }



        /// <summary>
        /// Reset dependency settings
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal override bool Reset(CacheRuntimeContext context)
        {
            base.Reset(context);
            _cdbSyncManager = context.ExpiryMgr.CacheDbSyncManager;
            _notifBasedDepManager = context.ExpiryMgr.NotifBasedDepManager;
            if (_notifBasedDepManager != null)
                return _notifBasedDepManager.Add(CacheKey, _conStr, _queryStr, context.IsDbSyncCoordinator , _hintType, _cmdType, _cmdParams,0);// initialize if this node is DbSyncCoordinator
            else
                return false;
        }

        internal override bool DetermineExpiration(CacheRuntimeContext context)
        {
            return false;
        }


        /// <summary>
        /// Base class uses this property to check for changes.
        /// </summary>
        public override Boolean HasChanged { get { return false; } }

        /// <summary>
        /// 
        /// </summary>
        protected override void DisposeInternal()
        {
            if (_cdbSyncManager != null)
            {
                _cdbSyncManager.RemoveFromSqlConnectionPool(_conStr);
            }

            if (_notifBasedDepManager != null)
            {
                _notifBasedDepManager.Remove(CacheKey);
            }

        }

        internal override bool ReInitializeHint(CacheRuntimeContext context)
        {
            return Reset(context); 
        }

        private string GetParametersString()
        {
            if (_cmdParams == null || _cmdParams.Count == 0) return "";

            StringBuilder sb = new StringBuilder();
            IDictionaryEnumerator ide = _cmdParams.GetEnumerator();
            while (ide.MoveNext())
            {
                sb.Append("\"" + ide.Key.ToString() + "\"");
                sb.Append((SqlCmdParams)ide.Value).ToString();
            }

            return sb.ToString();
        }

        private int CommandTypeToInt()
        {
            return (int)_cmdType;
        }

        public override string ToString()
        {
            string cmdParametersString = GetParametersString();
            return "YUKONDEPENDENCY \"" + _conStr + "\"" + _queryStr + "\"" + CommandTypeToInt().ToString() + "\"" + (cmdParametersString != "" ? "$cmd-params$" + GetParametersString() : "") + "\"\r\n";
        }

        #region ISizable Implementation

        public override int Size
        {
            get { return base.Size + SqlYukonCacheDependencySize; }
        }

        public override int InMemorySize
        {
            get
            {
                int inMemorySize = this.Size;

                inMemorySize += inMemorySize <= 24 ? 0 : Common.MemoryUtil.NetOverHead;

                return inMemorySize;
            }
        }

        private int SqlYukonCacheDependencySize
        {
            get
            {
                int temp = 0;
                temp += Common.MemoryUtil.NetReferenceSize; // for _cdbSyncManager
                temp += Common.MemoryUtil.NetReferenceSize;// for _notifBasedDepManager
                temp += Common.MemoryUtil.NetEnumSize; // for _cmdType

                if (_conStr != null)
                    temp += Common.MemoryUtil.GetStringSize(_conStr);

                if (_queryStr != null)
                    temp += Common.MemoryUtil.GetStringSize(_queryStr);

                if (_cmdParams != null)
                {
                    foreach (System.Collections.DictionaryEntry entry in _cmdParams)
                    {
                        String k = entry.Key as string;
                        SqlCmdParams param = entry.Value as SqlCmdParams;

                        temp += Common.MemoryUtil.GetStringSize(k);
                        temp += param.InMemorySize;
                    }
                }

                return temp;
            }
        }

        #endregion

        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _conStr = reader.ReadString();
            _queryStr = reader.ReadString();
            CacheKey = reader.ReadObject() as string;
            _cmdType = (CommandType)reader.ReadObject();
            _cmdParams = (IDictionary)reader.ReadObject();
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_conStr);
            writer.Write(_queryStr);
            writer.WriteObject(CacheKey);
            writer.WriteObject(_cmdType);
            writer.WriteObject(_cmdParams);
        }

        #endregion
    }
}