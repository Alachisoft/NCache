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

using System;
using System.Collections;
using System.Collections.Generic;

#if JAVA
using Alachisoft.TayzGrid.Runtime.Dependencies;
#else
using Alachisoft.NCache.Runtime.Dependencies;
#endif
#if JAVA
using Alachisoft.TayzGrid.Runtime;
#else
using Alachisoft.NCache.Runtime;
#endif
#if JAVA
using Alachisoft.TayzGrid.Runtime.Caching;
#else
using Alachisoft.NCache.Runtime.Caching;
#endif

#if JAVA
namespace Alachisoft.TayzGrid.Runtime.GenericTypesProvider
#else
namespace Alachisoft.NCache.Runtime.GenericTypesProvider
#endif
{

    /// <summary>
    /// Must be implemetd by the component which want to provide the 
    /// Generic Types of multiple parameters
    /// </summary>
    /// <remark>
    /// This Feature is Not Available in Express
    /// </remark>
  


	public interface IGenericTypeProvider
    {
        /// <summary>
        /// Client has to provide the implementation of this method, and he/she has to fill the array with the desire generic types and return it to us.
        /// </summary>
        /// <returns>System.Type[]</returns>
        Type[] GetGenericTypes();

        /// <summary>
        /// Provide custom implementation of this method to return true or false on the basis of whether you want to serialize the specific FieldInfo of specific Type
        /// </summary>
        /// <returns>bool</returns>

        bool CheckIfSerializable(Type type, System.Reflection.FieldInfo fieldInfo);
    }
}
