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
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.DataStructures;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// make it serializable coz cache operations performed through remoting will fail 
    /// otherwise.
    /// </summary>
    [Serializable]
    public class OperationContext : ICompactSerializable, ICloneable
    {
        private Hashtable _fieldValueTable;
        private static String s_operationUniqueID;
        private static long s_operationCounter;
        private static object s_lock = new object();

        static OperationContext()
        {
            s_operationUniqueID = Guid.NewGuid().ToString().Substring(0, 4);
        }

        public OperationContext() 
        {
            CreateOperationId();
            /*_fieldValueTable = new Hashtable();*/ 
        }

        public OperationContext(OperationContextFieldName fieldName, object fieldValue)
        {
            CreateOperationId();
            Add(fieldName, fieldValue);
        }

        private void CreateOperationId()
        {
            long opCounter = 0;
            lock (s_lock)
            {
                opCounter = s_operationCounter++;
            }
            OperationID operationId = new OperationID(s_operationUniqueID,opCounter);
            Add(OperationContextFieldName.OperationId, operationId);
        }

        public OperationID OperatoinID 
        {
            get { return (OperationID)GetValueByField(OperationContextFieldName.OperationId); } 
        }

      

        public void Add(OperationContextFieldName fieldName, object fieldValue)
        {
            lock (this)
            {
                if (_fieldValueTable == null)
                    _fieldValueTable = new Hashtable();

                _fieldValueTable[fieldName] = fieldValue;
            }
        }

        public object GetValueByField(OperationContextFieldName fieldName)
        {
            object result = null;

            if (_fieldValueTable != null)
                result = _fieldValueTable[fieldName];

            return result;
        }

        public bool Contains(OperationContextFieldName fieldName)
        {
            bool contains = false;

            if (_fieldValueTable != null)
                contains = _fieldValueTable.Contains(fieldName);

            return contains;
        }

        public void RemoveValueByField(OperationContextFieldName fieldName)
        {
            lock (this)
            {
                if (_fieldValueTable != null)
                    _fieldValueTable.Remove(fieldName);
            }
        }

        public bool IsOperation(OperationContextOperationType operationType)
        {
            if ((OperationContextOperationType)this.GetValueByField(OperationContextFieldName.OperationType) == operationType)
            { return true; }
            return false;
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _fieldValueTable = (Hashtable)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            lock (this)
            {
                writer.WriteObject(_fieldValueTable);
            }
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            OperationContext oc = new OperationContext();
            lock (this)
            {
                if (_fieldValueTable != null)
                {
                    if (oc._fieldValueTable != null) oc._fieldValueTable.Clear();
                    else oc._fieldValueTable = new Hashtable();

                    IDictionaryEnumerator ide = _fieldValueTable.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        Object clone = ide.Value is ICloneable ? ((ICloneable)ide.Value).Clone() : ide.Value;

                        oc._fieldValueTable.Add(ide.Key, clone);
                    }
                }
            }
            return oc;
        }

        #endregion
    }
    #region /                 --- OperationID ---           /

    #endregion

}
