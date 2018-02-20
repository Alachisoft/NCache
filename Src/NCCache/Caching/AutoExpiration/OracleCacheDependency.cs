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
    /// A notification based dependency. This dependency can be used only with Oracle 10g release 2 and above.
    /// Oracle itself notifies the client application if there are any changes
    /// in the data
    /// </summary>
    [Serializable]

    public class OracleCacheDependency : DependencyHint, ICompactSerializable
    {
        [NonSerialized]
        private CacheDbSyncManager _cdbSyncManager = null; // 8 bytes for ref
        [NonSerialized]

        private NotificationBasedDependencyManager _notifBasedDepManager = null; // 8 bytes for ref
        IDictionary _cmdParams;
        private CommandType _cmdType;//4 bytes

        private string _conStr;
        private string _queryStr;


        public OracleCacheDependency()
        {
            _hintType = ExpirationHintType.OracleCacheDependency;
        }
        /// <summary>
        /// Creates a OracleCacheDependency object based on a SqlCommand.
        /// </summary>
        /// <param name="conStr"></param>
        /// <param name="queryStr"></param>
        
        public OracleCacheDependency(string conStr, string queryStr)
            : this(conStr, queryStr, CommandType.Text, new Dictionary<string, OracleCommandParams>())
        { }
       
        public OracleCacheDependency(string conStr, string queryStr, CommandType cmdType, IDictionary cmdParams)
        {
            _hintType = ExpirationHintType.OracleCacheDependency;
            if (conStr == null || queryStr == null)
            {
                throw new ArgumentNullException("connection or query string is null");
            }
            _conStr = conStr;
            _queryStr = queryStr;
            _cmdType = cmdType;
            _cmdParams = cmdParams;
        }

        internal CommandType CommandType
        {
            get { return this._cmdType; }
        }

        public string ConnectionString
        {
            get { return this._conStr; }
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
            try
            {
                base.Reset(context);
                _cdbSyncManager = context.ExpiryMgr.CacheDbSyncManager;
                _notifBasedDepManager = context.ExpiryMgr.NotifBasedDepManager;
                if (_notifBasedDepManager != null)
                    return _notifBasedDepManager.Add(CacheKey, _conStr, _queryStr, context.IsDbSyncCoordinator, _hintType, _cmdType, _cmdParams,0);// initialize if this node is DbSyncCoordinator
                else
                    return false;
            }
            catch (System.IO.FileNotFoundException ex)
            {

                throw new Exception("Could not load assembly 'Oracle.DataAccess.dll'. Please make sure Oracle Data Provider for .NET is installed",ex);
            }
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

        private string GetParametersString()
        {
            if (_cmdParams == null || _cmdParams.Count == 0) return "";

            StringBuilder sb = new StringBuilder();
            IDictionaryEnumerator ide = _cmdParams.GetEnumerator();
            while (ide.MoveNext())
            {
                sb.Append("\"" + ide.Key.ToString() + "\"");
                sb.Append((OracleCommandParams)ide.Value).ToString();
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
            return "ORACLEDEPENDENCY \"" + _conStr + "\"" + _queryStr + "\"" + CommandTypeToInt().ToString() + "\"" + (cmdParametersString != "" ? "$cmd-params$" + GetParametersString() : "") + "\"\r\n";
        }

        #region ISizable Implementation

        public override int Size
        {
            get { return base.Size + OracleCacheDependencySize; }
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

        private int OracleCacheDependencySize
        {
            get {
                int temp = 0;
                temp+=Common.MemoryUtil.NetReferenceSize; // for _cdbSyncManager
                temp+=Common.MemoryUtil.NetReferenceSize;// for _notifBasedDepManager
                temp+=Common.MemoryUtil.NetEnumSize; // for _cmdType
                
                if(_conStr!=null)
                    temp+=Common.MemoryUtil.GetStringSize(_conStr);

                if(_queryStr!=null)
                    temp+=Common.MemoryUtil.GetStringSize(_queryStr);

                if (_cmdParams != null)
                {
                    foreach (System.Collections.DictionaryEntry entry in _cmdParams)
                    {
                        String k = entry.Key as string;
                        OracleCommandParams param = entry.Value as OracleCommandParams;

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
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_conStr);
            writer.Write(_queryStr);
            writer.WriteObject(CacheKey);
        }

        #endregion
    }
}