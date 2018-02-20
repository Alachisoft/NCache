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
    /// Gives the status of the completed task. 
    /// </summary>
 
    public enum TaskCompletionStatus
    {
        /// <summary>
        /// Task has completed successfully. 
        /// </summary>
        Success = 1,
        /// <summary>
        /// Taskfailed as either an exception has been thrown or time out has reached. 
        /// </summary>
        Failure = 2,
        /// <summary>
        /// Task has been cancelled.
        /// </summary>
        Cancelled = 3
    }
}
