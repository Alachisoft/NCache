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
using Alachisoft.NCache.Common.Interop;

namespace Alachisoft.NCache.Storage.Interop
{
    internal class SysUtil
    {
        public static uint AllignViewSize(uint viewSize)
        {
            SYSTEM_INFO pSI = new SYSTEM_INFO();
            Win32.GetSystemInfo(ref pSI);
            if (viewSize < (uint)pSI.dwAllocationGranularity)
                viewSize = (uint)pSI.dwAllocationGranularity;
            if (pSI.dwAllocationGranularity % viewSize > 0)
                viewSize += (uint)pSI.dwAllocationGranularity % viewSize;
            return viewSize;
        }
    }
}