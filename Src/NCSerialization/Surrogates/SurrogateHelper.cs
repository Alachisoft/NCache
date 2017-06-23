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
using System.Net;
using System.Collections;
using System.Web.SessionState;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;
using System.Web;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Serialization.Surrogates
{
    public class SurrogateHelper
    {
        public static object CreateGenericType(string name, params Type[] types)
        {
            string t = name + "`" + types.Length;
            Type generic = Type.GetType(t).MakeGenericType(types);
            return Activator.CreateInstance(generic);
        }
    }
}
