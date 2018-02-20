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
using Alachisoft.NCache.Runtime.Caching;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class SearchCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.SearchCommand _searchCommand;
        private int _methodOverload;

        public SearchCommand(string query, IDictionary values, bool searchEnteries, int methodOverload)
        {
            base.name = "SearchCommand";
            _searchCommand = new Alachisoft.NCache.Common.Protobuf.SearchCommand();
            _searchCommand.query = query;
            _searchCommand.searchEntries = searchEnteries;
            PopulateValues(values, _searchCommand.values);
            _methodOverload = methodOverload;
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
                        if (value == null)
                            throw new ArgumentNullException("NCache query does not support null values. ",
                                (System.Exception) null);
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

            if (value is Tag)
            {
                valueString = ((Tag) value).TagName;
            }

            return value.ToString();
        }

        internal override CommandType CommandType
        {
            get { return CommandType.SEARCH; }
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
            base._command.searchCommand = _searchCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.SEARCH;

            if (_searchCommand.searchEntries)
                base._command.commandVersion = 2;
            else
                base._command.commandVersion = 3;

            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.MethodOverload = _methodOverload;
        }
    }
}