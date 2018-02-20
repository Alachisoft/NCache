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
using Alachisoft.NCache.Runtime.Processor;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Processor
{
    [Serializable]
    public class EntryProcessorResult : IEntryProcessorResult, ICompactSerializable
    {
        private string _key = string.Empty;
        private object _value = null;
        private bool _isSuccessful = true;
        private EntryProcessorException _exception = null;
        private bool _exceptionOccured = false;

        public EntryProcessorResult(string key, object value)
        {
            _key = key;
            _value = value;
            _isSuccessful = true;
        }

        public EntryProcessorResult()
        { }

        public EntryProcessorResult(string key, EntryProcessorException exception)
        {
            _key = key;
            _exception = exception;
            _isSuccessful = false;
        }

        public object Value
        {
            get 
            {
                return _value; 
            }
        }

        public Exception Exception
        {
            get
            {
                return _exception;
            }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _value = reader.ReadObject();
            _exception = (EntryProcessorException)reader.ReadObject();
            _isSuccessful = (bool)reader.ReadBoolean();
            _key = (string)reader.ReadString();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_value);
            writer.WriteObject(_exception);
            writer.Write(_isSuccessful);
            writer.Write(_key);
        }

        public string Key
        {
            get { return _key; }
        }

        public bool IsSuccessful
        {
            get { return _isSuccessful; }
        }
    }
}
