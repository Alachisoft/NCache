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

namespace Alachisoft.NCache.MapReduce
{
    public enum MapReduceOpCodes
    {
        SubmitMapReduceTask = 1,
        CancelTask = 2,
        CancellAllTasks = 3,
        ReceiveReducerData = 4,
        MapperCompleted = 5,
        ReducerCompleted = 6,
        MapperFailed = 7,
        ReducerFailed = 8,
        GetTaskSequence = 9,
        RegisterTaskNotification = 10,
        UnregisterTaskNotification = 11,
        GetRunningTasks = 12,
        GetTaskStatus = 13,
        GetTaskEnumerator = 14,
        GetNextRecord = 15,
        StartTask = 16,
        RemoveFromSubmittedList = 17,
        RemoveFromRunningList = 18

    }
}
