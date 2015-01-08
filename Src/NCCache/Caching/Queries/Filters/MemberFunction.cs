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

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class MemberFunction : IFunctor, IComparable
    {
        string name;

        public MemberFunction(string memname)
        {
            name = memname;
        }

        public string MemberName
        {
            get { return name; }
        }

        public object Evaluate(object o)
        {
            object obj = o;
            System.Reflection.FieldInfo field = obj.GetType().GetField(name);
            if (field != null)
            {
                return field.GetValue(obj);
            }
            else
            {
                System.Reflection.PropertyInfo property = obj.GetType().GetProperty(name);
                if (property != null)
                {
                    return property.GetValue(obj, null);
                }
                else
                {
                    throw new ArgumentException(obj.GetType() +
                                                " contains no field or property named " +
                                                name);
                }
            }
        }

        public IIndexStore GetStore(AttributeIndex index)
        {
            if (index != null)
                return index.GetStore(name);
            return null;
        }

        public override string ToString()
        {
            return "GetMember(" + name + ")";
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is MemberFunction)
            {
                MemberFunction other = (MemberFunction)obj;
                return name.CompareTo(other.name);
            }
            return -1;
        }

        #endregion
    }
}
