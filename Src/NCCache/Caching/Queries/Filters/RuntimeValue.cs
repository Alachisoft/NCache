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

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class RuntimeValue : IGenerator, IComparable
    {

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is RuntimeValue)
                return 0;
            return -1;
        }

        #endregion

        #region IGenerator Members

        public object Evaluate()
        {
            return null;
        }

        public object Evaluate(string paramName, IDictionary vales)
        {
            object retVal = null;
            ArrayList list = vales[paramName] as ArrayList;
            if (list != null)
            {
                retVal = list[0];
                list.RemoveAt(0);
            }
            else
            {
                if (vales.Contains(paramName))
                    retVal = vales[paramName];
                else
                {
                    throw new ArgumentException("Provided data contains no field or property named " + paramName);
                }
            }

            return retVal;
        }

        #endregion
    }
}
