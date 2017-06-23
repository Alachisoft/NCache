// Copyright (c) 2017 Alachisoft
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
using System.Text;
using Alachisoft.NCache.Common.Protobuf;
using System.IO;

using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Web.Caching.Util;

using Alachisoft.NCache.Web.Communication;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class SearchCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.SearchCommand _searchCommand;

        public SearchCommand(string query, IDictionary values, bool searchEnteries)
        {
            base.name = "SearchCommand";
            _searchCommand = new Alachisoft.NCache.Common.Protobuf.SearchCommand();
            _searchCommand.query = query;
            _searchCommand.searchEntries = searchEnteries;            
            PopulateValues(values, _searchCommand.values); 
        }

        private void PopulateValues(IDictionary from, System.Collections.Generic.List<KeyValue> to)
        {
            IDictionaryEnumerator ide = from.GetEnumerator();
            ValueWithType valueWithType = null;
            KeyValue keyValue = null;

            while (ide.MoveNext())
            {
                keyValue = new KeyValue();
                keyValue.key = ide.Key.ToString();
                if (ide.Value == null)
                    throw new ArgumentException("NCache query does not support null values");

                if (ide.Value is ArrayList)
                {
                    ArrayList list = (ArrayList)ide.Value;
                    foreach (object value in list)
                    {
                        valueWithType = new ValueWithType();
                        valueWithType.value = GetValueString(value);
                        valueWithType.type = value.GetType().FullName;
                        
                        keyValue.value.Add(valueWithType);
                    }
                }
                else
                {
                    valueWithType = new ValueWithType();
                    valueWithType.value = GetValueString(ide.Value);
                    valueWithType.type = ide.Value.GetType().FullName;

                    keyValue.value.Add(valueWithType);
                }

                to.Add(keyValue);
            }
        }

        private string GetValueString(object value)
        {
            string valueString = string.Empty;

            // inform the user that null is not supported.
            if (value == null)
            throw new System.Exception("NCache query does not support null values");
            if (value is string) //Catter for case in-sensitive comparison
            {
                valueString = value.ToString().ToLower();
                return valueString;
            }

            if (value is DateTime)
            {
                valueString = ((DateTime)value).Ticks.ToString();
                return valueString;
            }

            return value.ToString();
        }

        internal override CommandType CommandType
        {
            get { return CommandType.SEARCH; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.BulkRead; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.searchCommand = _searchCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.SEARCH;
            base._command.commandVersion = 2;
            base._command.clientLastViewId = base.ClientLastViewId;
        }
    }
}
