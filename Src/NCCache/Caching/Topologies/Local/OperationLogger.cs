// Copyright (c) 2015 Alachisoft
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
using System.Collections;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    /// <summary>
    /// Logs the operation during state transfer
    /// </summary>
    class OperationLogger
    {
        LogMode _loggingMode;
        private int _bucketId;
        private Hashtable _logTbl;
        private bool _bucketTransfered = false;
        private RedBlack<HPTime> _opIndex;

        public OperationLogger(int bucketId, LogMode loggingMode)
        {
            _bucketId = bucketId;
            _opIndex = new RedBlack<HPTime>();
            _loggingMode = loggingMode;
        }

        public LogMode LoggingMode
        {
            get { return _loggingMode; }
            set { _loggingMode = value; }
        }

        public bool BucketTransfered
        {
            get { return _bucketTransfered; }
            set { _bucketTransfered = value; }
        }

        public Hashtable LoggedEnteries
        {
            get
            {
                Hashtable updatedKeys = null;
                Hashtable removedKeys = null;

                if (_logTbl == null)
                    _logTbl = new Hashtable();

                _logTbl["updated"] = new Hashtable();
                _logTbl["removed"] = new Hashtable();

                if (_logTbl.Contains("updated"))
                    updatedKeys = (Hashtable)_logTbl["updated"];

                if (_logTbl.Contains("removed"))
                    removedKeys = (Hashtable)_logTbl["removed"];

                IDictionaryEnumerator rbe = _opIndex.GetEnumerator();
                while (rbe.MoveNext())
                {
                    HashVector tbl = rbe.Value as HashVector;
                    OperationInfo info = null;

                    if (tbl != null)
                    {
                        IDictionaryEnumerator ide = tbl.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            info = (OperationInfo)ide.Key;
                            break;
                        }
                    }

                    switch ((int)info.OpType)
                    {
                        case (int)OperationType.Add:
                            removedKeys.Remove(info.Key);
                            updatedKeys[info.Key] = info.Entry;
                            break;

                        case (int)OperationType.Insert:
                            removedKeys.Remove(info.Key);
                            updatedKeys[info.Key] = info.Entry;
                            break;

                        case (int)OperationType.Delete:
                            updatedKeys.Remove(info.Key);
                            removedKeys[info.Key] = info.Entry;
                            break;
                    }
                }
                return _logTbl;
            }
        }

        public Hashtable LoggedKeys
        {
            get
            {
                ArrayList updatedKeys = null;
                ArrayList removedKeys = null;

                if (_logTbl == null)
                    _logTbl = new Hashtable();

                _logTbl["updated"] = new ArrayList();
                _logTbl["removed"] = new ArrayList();

                if (_logTbl.Contains("updated"))
                    updatedKeys = (ArrayList)_logTbl["updated"];

                if (_logTbl.Contains("removed"))
                    removedKeys = (ArrayList)_logTbl["removed"];

                IDictionaryEnumerator rbe = _opIndex.GetEnumerator();
                while (rbe.MoveNext())
                {
                    HashVector tbl = rbe.Value as HashVector;
                    OperationInfo info = null;

                    if (tbl != null)
                    {
                        IDictionaryEnumerator ide = tbl.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            info = (OperationInfo)ide.Key;
                            break;
                        }
                    }

                    switch ((int)info.OpType)
                    {
                        case (int)OperationType.Add:
                            removedKeys.Remove(info.Key);
                            updatedKeys.Add(info.Key);
                            break;

                        case (int)OperationType.Insert:
                            removedKeys.Remove(info.Key);
                            updatedKeys.Add(info.Key);
                            break;

                        case (int)OperationType.Delete:
                            updatedKeys.Remove(info.Key);
                            removedKeys.Add(info.Key);
                            break;
                    }
                }
                return _logTbl;
            }
        }

        public void Clear()
        {
            if (_opIndex != null)
                _opIndex.Clear();
        }

        public void LogOperation(object key, CacheEntry entry, OperationType type)
        {
            if (_opIndex != null)
                _opIndex.Add(HPTime.Now, new OperationInfo(key, entry, type));
        }
    }
}
