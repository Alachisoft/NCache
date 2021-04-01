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
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.ContentOptimization.Diagnostics.Logging
{
    public enum TraceSeverity
    {
        Unassigned = 0,
        CriticalEvent = 1,
        WarningEvent = 2,
        InformationEvent = 3,
        Exception = 4,
        Unexpected = 10,
        Monitorable = 15,
        High = 20,
        Medium = 50,
        Verbose = 100,
    }
}
