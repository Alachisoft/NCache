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
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Runtime.Aggregation
{
    /// <summary>
    /// Implements the interface to extract the meaningful attributes from given objects, Similar to Mapper in MapReduce framework.
    /// </summary>
    public interface IValueExtractor
    {
        /// <summary>
        /// Contains the logic to extract meaningful information/attributes fromm the given object. 
        /// </summary>
        /// <param name="value">Value / Object </param>
        /// <returns>Returns the extracted value, which can also be null. </returns>
        /// <example>
        /// Following example demonstrates the implementation of Extract. 
        /// <code>
        /// public object Extract(object value)
        /// {
        ///    try
        ///    {
        ///       if (value.GetType() == typeof(int))
        ///        {
        ///            return 0;
        ///        }
        ///       if (value.GetType() == typeof(float))
        ///        {
        ///            return 0.0;
        ///        }
        ///    }
        ///    catch (Exception e)
        ///    {
        ///       //handle exception
        ///    }
        ///    return value;
        /// }
        /// </code>
        /// </example>
        object Extract(object value);

    }
}
