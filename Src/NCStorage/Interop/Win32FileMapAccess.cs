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

namespace Alachisoft.NCache.Storage.Interop
{
    [Flags]
    //[CLSCompliant(false)]
    internal enum Win32FileMapAccess : uint
    {
        FILE_MAP_COPY = 0x0001,
        FILE_MAP_WRITE = 0x0002,
        FILE_MAP_READ = 0x0004,
        FILE_MAP_ALL_ACCESS = 0x000F0000 
                              | FILE_MAP_COPY
                              | FILE_MAP_WRITE 
                              | FILE_MAP_READ 
                              | 0x0008 
                              | 0x0010
    }
}