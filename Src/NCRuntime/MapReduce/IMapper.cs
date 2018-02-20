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
using System.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Runtime.MapReduce
{
    /// <summary>
    /// MapReduce mapper interface generates a set of intermediate key-value pairs for further refining and extraction of the data.
    /// </summary>
    public interface IMapper : IDisposable
    {
        /// <summary>
        /// For every key-value pair input, Map method is executed, to get a more specific and meaningful data. 
        /// </summary>
        /// <param name="key">Key value of cache Entry.</param>
        /// <param name="value">Value for the key</param>
        /// <param name="context">Emitted output value for each key-value pair</param>
        /// <example>
        /// Following example demonstrate the usage of Map. 
        /// <code>
        /// string[] parsedline;
        /// string line;
        /// public void Map(Object key, Object value, IOutputMap context)
        /// {
        ///     line = value.ToString();
        ///     parsedline = line.Split(' ');
        ///     for (int i = parsedline.Length; i>=0; i++)
        ///      {
        ///        context.Emit(parsedline[i], 1);
        ///       }
        ///   }
        /// </code>
        /// </example>
        void Map(object key, object value, IOutputMap context);
    }
}
