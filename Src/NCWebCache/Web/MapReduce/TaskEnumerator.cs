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
using System.Collections.Generic;
using System.Collections;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Web.MapReduce
{
    public class TaskEnumerator : IDictionaryEnumerator
    {
        List<TaskPartitionedEnumerator> _partitionedEnumerators;
        TaskPartitionedEnumerator _currentPartitionedEnumerator;
        int _roundRobinIndexer = 0;
        private bool _isValid = false;
        private bool hasNext = false;

        private DictionaryEntry currentEntry = new DictionaryEntry();
        
        public TaskEnumerator(List<Common.MapReduce.TaskEnumeratorResult> enumeratorResultSet,
            TaskEnumeratorHandler remoteCache)
        {
            _partitionedEnumerators = new List<TaskPartitionedEnumerator>();
            foreach (Common.MapReduce.TaskEnumeratorResult result in enumeratorResultSet)
            {
                TaskPartitionedEnumerator mrResultPEnumerator = new TaskPartitionedEnumerator(remoteCache,
                    result.Pointer,
                    result.RecordSet,
                    result.NodeAddress,
                    result.IsLastResult);
                _partitionedEnumerators.Add(mrResultPEnumerator);
            }

            ValidatePEnumerator();
        }

        #region IDictionaryEnumerator Members

        public DictionaryEntry Entry
        {
            get
            {
                try
                {
                    if (_isValid && hasNext)
                    {
                        return _currentPartitionedEnumerator.Entry;
                    }
                    else
                    {
                        throw new OperationFailedException("Enumeration has either not started or already finished.");
                    }
                }
                finally
                {
                    ValidatePEnumerator();
                }
            }
        }

        public object Key
        {
            get
            {
                try
                {
                    if (_isValid && hasNext)
                    {
                        return _currentPartitionedEnumerator.Key;
                    }
                    else
                    {
                        throw new OperationFailedException("Enumeration has either not started or already finished.");
                    }
                }
                finally
                {
                    ValidatePEnumerator();
                }
            }
        }

        public object Value
        {
            get
            {
                try
                {
                    if (_isValid && hasNext)
                    {
                        return _currentPartitionedEnumerator.Value;
                    }
                    else
                    {
                        throw new OperationFailedException("Enumeration has either not started or already finished.");
                    }
                }
                finally
                {
                    ValidatePEnumerator();
                }
            }
        }

        #endregion

        #region IEnumerator Members

        public object Current
        {
            get
            {
                try
                {
                    if (_isValid && hasNext)
                    {
                        return _currentPartitionedEnumerator.Current;
                    }
                    else
                    {
                        throw new OperationFailedException("Enumeration has either not started or already finished.");
                    }
                }
                finally
                {
                    ValidatePEnumerator();
                }
            }
        }

        public bool MoveNext()
        {
            bool next = false;
            try
            {
                next = _currentPartitionedEnumerator.MoveNext();
            }
            catch (Exception ex)
            {
                InValidatePEnumerator();
                throw ex;
            }

            hasNext = next;
            return next;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        #endregion

        private void ValidatePEnumerator()
        {
            int invalidEnumerators = 0;
            do
            {
                //if loop is iterated more times than _partitionedEnumerator's size, break the loop
                if (invalidEnumerators >= _partitionedEnumerators.Count)
                {
                    break;
                }

                if (_partitionedEnumerators != null || _partitionedEnumerators.Count != 0)
                {
                    _roundRobinIndexer = (++_roundRobinIndexer) % _partitionedEnumerators.Count;
                    _currentPartitionedEnumerator = _partitionedEnumerators[_roundRobinIndexer];

                    if (!_currentPartitionedEnumerator.IsValid)
                    {
                        invalidEnumerators++;
                    }
                    else
                    {
                        _isValid = true;
                        break;
                    }
                }
            } while (true);
        }

        private void InValidatePEnumerator()
        {
            foreach (TaskPartitionedEnumerator pe in _partitionedEnumerators)
            {
                pe.IsValid = false;
            }

            _isValid = false;
        }
    }
}