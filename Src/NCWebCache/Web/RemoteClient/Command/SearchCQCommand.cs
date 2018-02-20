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
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Web.Caching.Util;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class SearchCQCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.SearchCQCommand _searchCommand;
        private int _methodOverload;

        public SearchCQCommand(string query, IDictionary values, string clientUniqueId, bool searchEntries,
            bool notifyAdd, bool notifyUpdate, bool notifyRemove, int addDF, int removeDF, int updateDF,
            int methodOverload)
        {
            base.name = "SearchCQCommand";
            _searchCommand = new Alachisoft.NCache.Common.Protobuf.SearchCQCommand();
            _searchCommand.query = query;
            _searchCommand.searchEntries = searchEntries;
            _searchCommand.notifyAdd = notifyAdd;
            _searchCommand.notifyUpdate = notifyUpdate;
            _searchCommand.notifyRemove = notifyRemove;
            _searchCommand.clientUniqueId = clientUniqueId;

            _searchCommand.addDataFilter = addDF;
            _searchCommand.remvoeDataFilter = removeDF;
            _searchCommand.updateDataFilter = updateDF;
            _methodOverload = methodOverload;
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
                    ArrayList list = (ArrayList) ide.Value;
                    foreach (object value in list)
                    {
                        Type type = value.GetType();
                        bool isTag = CommandHelper.IsTag(type);
                        if (!(CommandHelper.IsIndexable(type) || isTag))
                            throw new ArgumentException("The provided type is not indexable. ", type.Name);
                        valueWithType = new ValueWithType();
                        valueWithType.value = GetValueString(value);
                        if (isTag)
                            valueWithType.type = typeof(string).FullName;
                        else
                            valueWithType.type = value.GetType().FullName;

                        keyValue.value.Add(valueWithType);
                    }
                }
                else
                {
                    Type type = ide.Value.GetType();
                    bool isTag = CommandHelper.IsTag(type);
                    if (!(CommandHelper.IsIndexable(type) || isTag))
                        throw new ArgumentException("The provided type is not indexable. ", type.Name);
                    valueWithType = new ValueWithType();
                    valueWithType.value = GetValueString(ide.Value);
                    if (isTag)
                        valueWithType.type = typeof(string).FullName;
                    else
                        valueWithType.type = ide.Value.GetType().FullName;
                    keyValue.value.Add(valueWithType);
                }

                to.Add(keyValue);
            }
        }

        private string GetValueString(object value)
        {
            string valueString = String.Empty;


            if (value == null)

                throw new System.Exception("NCache query does not support null values");

            if (value is System.String)
            {
                valueString = value.ToString().ToLower();
                return valueString;
            }

            if (value is System.DateTime)
            {
                valueString = ((DateTime) value).Ticks.ToString();
                return valueString;
            }

            return value.ToString();
        }

        internal override CommandType CommandType
        {
            get { return CommandType.SEARCH_CQ; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.NonKeyBulkRead; }
        }

        internal override bool IsKeyBased
        {
            get { return false; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.searchCQCommand = _searchCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.SEARCH_CQ;
            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.MethodOverload = _methodOverload;
        }
    }
}