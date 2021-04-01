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
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Caching;

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// make it serializable coz cache operations performed through remoting will fail 
    /// otherwise.
    /// Class is Serailizble for whenever remoting is used
    /// </summary>
    public class EventContext : ICompactSerializable, ICloneable
    {
        private Hashtable _fieldValueTable;
        private string _uniqueId="";
        private string _cqUniqueId="";
        public EventCacheEntry Item
        {
            get { return (EventCacheEntry)this.GetValueByField(EventContextFieldName.EventCacheEntry); }
            set { Add(EventContextFieldName.EventCacheEntry, value); }
        }

        public EventCacheEntry OldItem
        {
            get { return (EventCacheEntry)this.GetValueByField(EventContextFieldName.OldEventCacheEntry); }
            set { Add(EventContextFieldName.OldEventCacheEntry, value); }
        }

      

        public object CollectionItem
        {
            get { return GetValueByField(EventContextFieldName.CollectionEventItem); }
            set { Add(EventContextFieldName.CollectionEventItem, value); }
        }

        public object OldCollectionItem
        {
            get { return GetValueByField(EventContextFieldName.OldCollectionEventItem); }
            set { Add(EventContextFieldName.OldCollectionEventItem, value); }
        }

        public string TaskFailureReason
        {
            get { return (string)this.GetValueByField(EventContextFieldName.TaskFailureReason); }
            set { Add(EventContextFieldName.TaskFailureReason, value); }
        }

        public EventContext() {  }

        public EventContext(EventContextFieldName fieldName, object fieldValue)
        {
            Add(fieldName, fieldValue);
        }

        public void Add(EventContextFieldName fieldName, object fieldValue)
        {
            lock (this)
            {
                if (_fieldValueTable == null)
                    _fieldValueTable = new Hashtable();

                _fieldValueTable[fieldName] = fieldValue;
            }
        }

        public object GetValueByField(EventContextFieldName fieldName)
        {
            object result = null;

            if (_fieldValueTable != null)
                result = _fieldValueTable[fieldName];

            return result;
        }

        public bool Contains(EventContextFieldName fieldName)
        {
            bool contains = false;

            if (_fieldValueTable != null)
                contains = _fieldValueTable.Contains(fieldName);

            return contains;
        }

        public void RemoveValueByField(EventContextFieldName fieldName)
        {
            lock (this)
            {
                if (_fieldValueTable != null)
                    _fieldValueTable.Remove(fieldName);
            }
        }

        public bool HasEventID(EventContextOperationType operationType)
        {
            if (this.GetValueByField(EventContextFieldName.EventID) != null)
            { return true; }
            return false;
        }

        public EventId EventID
        {
            get
            {
                return (EventId)this.GetValueByField(EventContextFieldName.EventID);
            }

            set
            {
                Add(EventContextFieldName.EventID, value);
            }
        }

        public string UniqueId
        {
            get
            {
                return _uniqueId;
            }

            set
            {
                _uniqueId = value;
            }
        }
        public string CQUniqueId
        {
            get
            {
                return _cqUniqueId;
            }

            set
            {
                _cqUniqueId = value;
            }
        }
        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _fieldValueTable = (Hashtable)reader.ReadObject();
            _uniqueId = (string)reader.ReadObject();
            _cqUniqueId =(string) reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            lock (this)
            {
                writer.WriteObject(_fieldValueTable);
                writer.WriteObject(_uniqueId);
                writer.WriteObject(_cqUniqueId);
            }
        }

        #endregion

        #region ICloneable Members

        /// <summary>
        /// Deep clone.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            EventContext oc = new EventContext();
            lock (this)
            {
                if (oc._fieldValueTable == null) oc._fieldValueTable = new Hashtable();
                else oc._fieldValueTable.Clear();

                if (_fieldValueTable != null)
                {
                    IDictionaryEnumerator ide = _fieldValueTable.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        Object clone = ide.Value is ICloneable ? ((ICloneable)ide.Value).Clone() : ide.Value;

                        oc._fieldValueTable.Add(ide.Key, clone);
                    }
                }
                oc.UniqueId =String.Copy(UniqueId);
                oc.CQUniqueId =String.Copy(CQUniqueId);

            }
            return oc;
        }

        #endregion

    }
}
