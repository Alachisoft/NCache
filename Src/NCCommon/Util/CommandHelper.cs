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
// limitations under the License

using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Runtime.Caching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Util
{
    public class CommandHelper
    {
        public static void PopulateValues(IDictionary from, System.Collections.Generic.List<KeyValue> to)
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
                        if (value == null)
                            throw new ArgumentNullException("NCache query does not support null values. ",
                                (System.Exception)null);
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

        private static string GetValueString(object value)
        {
            string valueString = String.Empty;

            if (value == null)
#if JAVA
                throw new System.Exception("TayzGrid query does not support null values");
#else
                throw new System.Exception("NCache query does not support null values");
#endif
            if (value is System.String)
            {
                valueString = value.ToString().ToLower();
                return valueString;
            }

            if (value is System.DateTime)
            {
                valueString = ((DateTime)value).Ticks.ToString();
                return valueString;
            }

            if (value is Tag)
            {
                valueString = ((Tag)value).TagName;
            }

            return value.ToString();
        }

        private static bool IsIndexable(Type type)
        {
            return (type.IsPrimitive || type.Equals(typeof(string)) || type.Equals(typeof(DateTime)) ||
                    type.Equals(typeof(Decimal)) || type.Equals(typeof(Decimal)));
        }

        private static bool IsTag(Type type)
        {
            return type.Equals(typeof(Tag));
        }

        public static bool Queable(object command)
        {
            if (!(command is Common.Protobuf.Command))
            {
                return false;
            }

            Common.Protobuf.Command cmd = (Common.Protobuf.Command)command;

            switch (cmd.type)
            {
                case Common.Protobuf.Command.Type.ADD:
                case Common.Protobuf.Command.Type.INSERT:
                case Common.Protobuf.Command.Type.REMOVE:
                case Common.Protobuf.Command.Type.DELETE:
                case Common.Protobuf.Command.Type.CONTAINS:
                case Common.Protobuf.Command.Type.GET:
                case Common.Protobuf.Command.Type.COUNT:
                    return true;
            }

            return false;
        }

    }
}
