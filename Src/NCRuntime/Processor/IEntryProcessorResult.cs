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

namespace Alachisoft.NCache.Runtime.Processor
{
    /// <summary>
    /// Returns updated data or exception after execution of entry processor. 
    /// </summary>
    public interface IEntryProcessorResult
    {
        /// <summary>
        /// Returns the key for cache entry.
        /// </summary>
        string Key { get; }
        /// <summary>
        /// Returns the custom result of IEntryProcessor.
        /// </summary>
        object Value { get; }
        /// <summary>
        /// True if no internal or external exception has been thrown by the processer. IEntryProcesser results will be returned only if process executon has been successful.
        /// </summary>
        bool IsSuccessful { get; }
        /// <summary>
        /// Holds internal or external exception.
        /// </summary>
        Exception Exception { get; }
    }
}
