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
    ///Assigns a unique Combiner for each provided key.
    /// </summary>
    public interface ICombinerFactory
    {
        /// <summary>
        ///  Provides incoming element with a new instance of Combiner to merge intermediate key-value pairs from Mapper.
        /// </summary>
        /// <param name="key">Key for new Combiner</param>
        /// <returns>New instance of ICombiner.</returns>
        /// <example>
        /// Following example demostrates the implementation of create.
        /// <code>
        /// public ICombiner Create(object key)
        ///  {
        ///      WordCountCombiner wcCombiner = new WordCountCombiner(); //new instance of combiner.
        ///      return wcCombiner;
        ///  }
        /// </code>
        /// </example>
        ICombiner Create(object key);
    }
}
