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
// limitations under the License.

using System;

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Defines a callback method for notifying application when an item is added, updated or removed from the Continuous Query result.
    /// </summary>
    /// <param name="key">key removed, add or updated</param>
    /// <param name="arg">Information about continuous query event arguments</param>
    public delegate void QueryDataNotificationCallback(String key, CQEventArg arg);
}