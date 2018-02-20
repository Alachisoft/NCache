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
    [Serializable]
    public class NosDBCacheDependency: DependencyHint, ICompactSerializable
    {
        private string  _conStr;
        private string  _queryStr;
        private IDictionary _cmdParams;
        private int   _timeOut;

        [NonSerialized] private NotificationBasedDependencyManager _notifBasedDepManager;

        public NosDBCacheDependency()
        {
            _hintType = ExpirationHintType.NosDBCacheDependency;
        }
        /// <summary>
        /// Creates a NosDBCacheDependency object based on a SqlCommand.
        /// </summary>
        /// <param name="conStr"></param>
        /// <param name="queryStr"></param>

        public NosDBCacheDependency(string conStr, string queryStr)
            : this(conStr, queryStr, 0, new Dictionary<string, object>())
        { }

        public NosDBCacheDependency(string conStr, string queryStr, int timeout, IDictionary cmdParams)
        {
            _hintType = ExpirationHintType.NosDBCacheDependency;

            if (conStr == null || queryStr == null)
            {
                throw new ArgumentNullException("connection or query string is null");
            }

            _conStr = conStr;
            _queryStr = queryStr;
            _timeOut = timeout;
            _cmdParams = cmdParams;
        }

        internal string ConnectionString
        {
            get { return _conStr; }
        }

        internal int Timeout
        {
            get { return _timeOut; }
        }

        internal string QueryString
        {
            get { return _queryStr; }
        }

        internal IDictionary CommandParams
        {
            get { return _cmdParams; }
        }

        /// <summary>
        /// Reset dependency settings
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal override bool Reset(CacheRuntimeContext context)
        {
            base.Reset(context);
            // todo add manager code for handling NosDB dependency  
            _notifBasedDepManager = context.ExpiryMgr.NotifBasedDepManager;
            if (_notifBasedDepManager != null)
                return _notifBasedDepManager.Add(CacheKey, _conStr, _queryStr, context.IsDbSyncCoordinator, _hintType, CommandType.Text, _cmdParams, _timeOut);// initialize if this node is DbSyncCoordinator
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
                sb.Append("\"" + ide.Key + "\"");
                sb.Append(ide.Value);
            }

            return sb.ToString();
        }

       
        public override string ToString()
        {
            string cmdParametersString = GetParametersString();
            return "NosDBDEPENDENCY \"" + _conStr + "\"" + _queryStr + "\"" + _timeOut + "\"" + (cmdParametersString != "" ? "$cmd-params$" + GetParametersString() : "") + "\"\r\n";
        }

        #region ISizable Implementation

        public override int Size
        {
            get { return base.Size + NosDbCacheDependencySize; }
        }

        public override int InMemorySize
        {
            get
            {
                int inMemorySize = Size;

                inMemorySize += inMemorySize <= 24 ? 0 : Common.MemoryUtil.NetOverHead;

                return inMemorySize;
            }
        }

        private int NosDbCacheDependencySize
        {
            get
            {
                int temp = 0;
                temp += Common.MemoryUtil.NetIntSize; // for _timeOut

                if (_conStr != null)
                    temp += Common.MemoryUtil.GetStringSize(_conStr);

                if (_queryStr != null)
                    temp += Common.MemoryUtil.GetStringSize(_queryStr);

                if (_cmdParams != null)
                {
                    foreach (DictionaryEntry entry in _cmdParams)
                    {
                        String k = entry.Key as string;
                        SqlCmdParams param = entry.Value as SqlCmdParams;

                        temp += Common.MemoryUtil.GetStringSize(k);
                        if (param != null) temp += param.InMemorySize;
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
            _timeOut = (int)reader.ReadObject();
            _cmdParams = (IDictionary)reader.ReadObject();
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_conStr);
            writer.Write(_queryStr);
            writer.WriteObject(CacheKey);
            writer.WriteObject(_timeOut);
            writer.WriteObject(_cmdParams);
        }

        #endregion
    }
}
