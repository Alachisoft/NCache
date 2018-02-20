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
    internal sealed class ExecuteReader : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.ExecuteReaderCommand _raederCommand;
        private int _methodOverload;

        public ExecuteReader(string query, IDictionary values, bool getData, int chunkSize, int methodOverload)
        {
            base.name = "ExecuteReaderCommand";
            _raederCommand = new Alachisoft.NCache.Common.Protobuf.ExecuteReaderCommand();
            _raederCommand.query = query;
            _raederCommand.getData = getData;
            _raederCommand.chunkSize = chunkSize;
            PopulateValues(values, _raederCommand.values);
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
                    if (ide.Value == null)
                        throw new ArgumentNullException("NCache query does not support null values. ",
                            (System.Exception) null);
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
            get { return CommandType.EXECUTE_READER; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.NonKeyBulkRead; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.executeReaderCommand = _raederCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.EXECUTE_READER;
            base._command.commandVersion = 1;
            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.MethodOverload = _methodOverload;
        }
    }
}