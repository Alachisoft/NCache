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
    ///Assigns a unique Reducer for each provided key.
    ///Implementing IReducerFactory is optional.
    /// </summary>
    public interface IReducerFactory
    {
        /// <summary>
        /// Provides incoming element with a new instance of Reducer to merge intermediate key-value pairs from Combiner.
        /// </summary>
        /// <param name="key">Key for new Reducer</param>
        /// <returns>New instance of IReducer.</returns>
        /// <example>
        /// Following example demostrates the implementation of Create.
        /// <code>
        /// public IReducer Create(object key)
        ///  {
        ///      WordCountReducer wcReducer = new WordCountReducer(); //new instance of Reducer.
        ///      return wcReducer;
        ///  }
        /// </code>
        /// </example>
        IReducer Create(object key);
    }
}
