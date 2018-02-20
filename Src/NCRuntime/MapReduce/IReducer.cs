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
    /// Apply aggregation and compilation on final result. 
    /// </summary>
    public interface IReducer : IDisposable
    {
        /// <summary>
        /// Starting point for initialization of reducer.
        /// </summary>
        /// <example>
        /// Following example illustrate the usage of BeginReduce. 
        /// <code>
        ///  public void BeginReduce()
        ///  {
        ///    // Initialization
        ///  }
        /// </code>
        /// </example>
        void BeginReduce();
        /// <summary>
        /// Reduces the key-value pair to further meaning full pairs.
        /// </summary>
        /// <param name="value">Value for the specified key. </param>
        /// <example>
        /// Following example illustrate the usage of Reduce. 
        /// <code>
        /// public void Reduce(object value)
        /// {
        ///   count += int.Parse(value.ToString());
        /// }
        /// </code>
        /// </example>
        void Reduce(object value);
        /// <summary>
        /// Provides final result of map reduce task. 
        /// </summary>
        /// <returns>Return key-value pair.</returns>
        /// <example>
        /// <code>
        /// public KeyValuePair FinishReduce()
        /// {
        ///   KeyValuePair kvp = null;
        ///   kvp.Key = key;
        ///   kvp.Value = count;
        ///   return kvp;
        ///  }
        /// </code>
        /// </example>
        Alachisoft.NCache.Runtime.MapReduce.KeyValuePair FinishReduce();
    }
}
