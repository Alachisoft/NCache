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

namespace Alachisoft.NCache.Runtime.MapReduce
{
    /// <summary>
    /// Allows user to filter the data before providing it to Mapper.
    /// <remarks><b>Note:</b> if filter is not applied than whole cache data will be mapped. </remarks>
    /// </summary>
    public interface IKeyFilter
    {
        /// <summary>
        /// Map will be executed on specified key if ture is returned. 
        /// </summary>
        /// <param name="key">Key for filtering </param>
        /// <returns>Returns if map has to be applied or not.</returns>
        /// <example>
        /// Following example illustrate the usage of FilterKey. 
        /// <code>
        /// public class MapReduceKeyFilter : IKeyFilter
        /// {
        ///  public bool FilterKey(object key)
        ///   {
        ///    try
        ///    {
        ///        if (key.ToString().Contains("hungry"))
        ///        {
        ///            return true;
        ///        }
        ///
        ///    }
        ///    catch (Exception exp)
        ///    {
        ///        //handle exception
        ///    }
        ///    return false;
        ///   }
        /// }
        /// </code>
        /// </example>
        bool FilterKey(object key);
    }
}
