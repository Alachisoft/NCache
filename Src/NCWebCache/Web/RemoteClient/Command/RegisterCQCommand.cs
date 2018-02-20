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

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class RegisterCQCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.RegisterCQCommand _cqCommand;
        private int _methodOverload;

        public RegisterCQCommand(string query, IDictionary values, string clientUniqueId, bool notifyAdd,
            bool notifyUpdate, bool notifyRemove, int addDF, int removeDF, int updateDF, int methodOverload)
        {
            base.name = "RegisterCQCommand";
            _cqCommand = new Alachisoft.NCache.Common.Protobuf.RegisterCQCommand();
            _cqCommand.query = query;
            _cqCommand.notifyAdd = notifyAdd;
            _cqCommand.notifyUpdate = notifyUpdate;
            _cqCommand.notifyRemove = notifyRemove;
            _cqCommand.clientUniqueId = clientUniqueId;

            _cqCommand.addDataFilter = addDF;
            _cqCommand.remvoeDataFilter = removeDF;
            _cqCommand.updateDataFilter = updateDF;
            _methodOverload = methodOverload;
            PopulateValues(values, _cqCommand.values);
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
                System.Globalization.CultureInfo enUs = new System.Globalization.CultureInfo("en-US");
                valueString = ((DateTime) value).ToString(enUs);
                return valueString;
            }

            return value.ToString();
        }

        internal override CommandType CommandType
        {
            get { return CommandType.REGISTER_CQ; }
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
            base._command.registerCQCommand = _cqCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_CQ;
            base._command.MethodOverload = _methodOverload;
        }
    }
}