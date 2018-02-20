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
using Alachisoft.NCache.Common.MapReduce;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Web.MapReduce
{
    internal class TaskPartitionedEnumerator : IDictionaryEnumerator
    {
        private TaskEnumeratorPointer pointer = null;
        private DictionaryEntry currentRecordSet = new DictionaryEntry();
        private DictionaryEntry nextRecordSet = new DictionaryEntry();
        private string nodeAddress = null;
        private bool isLastResult = false;
        private TaskEnumeratorHandler handler = null;
        private short callbackId;
        private bool isValid = false;

        public TaskPartitionedEnumerator(TaskEnumeratorHandler remoteCache, TaskEnumeratorPointer pntr,
            DictionaryEntry entry, string nodeaddress, bool isLast)
        {
            this.handler = remoteCache;
            this.pointer = pntr;
            this.nextRecordSet = entry;
            this.nodeAddress = nodeaddress;
            this.isLastResult = isLast;

            this.callbackId = pointer.CallbackId;

            if (nextRecordSet.Key != null)
                isValid = true;
        }

        public TaskEnumeratorPointer Pointer
        {
            get { return pointer; }
            set { pointer = value; }
        }

        public bool IsValid
        {
            get { return isValid; }
            set { isValid = value; }
        }

        public string NodeAddress
        {
            get { return nodeAddress; }
            set { nodeAddress = value; }
        }

        public bool IsLastResult
        {
            get { return isLastResult; }
            set { isLastResult = value; }
        }

        public DictionaryEntry RecordSet
        {
            get { return currentRecordSet; }
            set { currentRecordSet = value; }
        }

        #region IDictionaryEnumerator Members

        public DictionaryEntry Entry
        {
            get { return currentRecordSet; }
        }

        public object Key
        {
            get { return currentRecordSet.Key; }
        }

        public object Value
        {
            get { return currentRecordSet.Value; }
        }

        #endregion


        public object Current
        {
            get { return currentRecordSet; }
        }

        public bool MoveNext()
        {
            if (IsValid)
            {
                currentRecordSet = nextRecordSet; // set the current record set.

                if (!IsLastResult)
                {
                    try
                    {
                        TaskEnumeratorResult enumeratorResultSet = null;
                        enumeratorResultSet = handler.NextRecord(pointer.ClusterAddress.IpAddress.ToString(), pointer);

                        if (enumeratorResultSet != null)
                        {
                            nextRecordSet = enumeratorResultSet.RecordSet;

                            IsLastResult = enumeratorResultSet.IsLastResult;
                            isValid = true;
                        }
                    }
                    catch (OperationFailedException ex)
                    {
                        isValid = false;
                        throw new Exception("Output corrupted on node : " + pointer.ClientAddress.ToString() +
                                            "and Exception is :" + ex);
                    }
                }
                else
                {
                    isValid = false;
                    // Means enumerator has the last entry.
                    // Return True for that last entry
                    return true;
                }
            }

            return IsValid;
        }

        public void Reset()
        {
        }
    }
}