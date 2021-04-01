//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
namespace Alachisoft.NCache.SocketServer.EventTask
{
    /// <summary>
    /// On happening of an event, an event task is stored in a Queue, which when dequeued
    /// is processed to send appropriate command back to clients who have registered the event
    /// </summary>
    internal interface IEventTask
    {
        /// <summary>
        /// Process the event task
        /// </summary>
        void Process();
    }
}
