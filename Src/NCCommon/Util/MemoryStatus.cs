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
using System.Runtime.InteropServices;

namespace Alachisoft.NCache.Common.Util
{
    public static class MemoryStatus
    {      
        public struct MemoryStatusEx
        {
            /// <summary>
            /// The size of the structure, in bytes.
            /// </summary>
            [CLSCompliant(false)]
            public uint Length;

            /// <summary>
            /// A number between 0 and 100 that specifies the approximate percentage of 
            /// physical memory that is in use
            /// </summary>
            [CLSCompliant(false)]
            public uint MemoryLoad;

            /// <summary>
            /// The amount of actual physical memory, in bytes.
            /// </summary>
            [CLSCompliant(false)]
            public ulong TotalMemory;

            /// <summary>
            /// The amount of physical memory currently available, in bytes. 
            /// This is the amount of physical memory that can be immediately 
            /// reused without having to write its contents to disk first. 
            /// </summary>
            [CLSCompliant(false)]
            public ulong AvailableMemory;

            /// <summary>
            /// The current committed memory limit for the system or 
            /// the current process, whichever is smaller, in bytes.
            /// </summary>
            [CLSCompliant(false)]
            public ulong TotalPageFile;

            /// <summary>
            /// The maximum amount of memory the current process can commit, in bytes.
            /// </summary>
            [CLSCompliant(false)]
            public ulong AvailablePageFile;

            /// <summary>
            /// The size of the user-mode portion of the virtual address space of the 
            /// calling process, in bytes. This value depends on the type of process, 
            /// the type of processor, and the configuration of the operating system. 
            /// For example, this value is approximately 2 GB for most 32-bit processes 
            /// on an x86 processor and approximately 3 GB for 32-bit processes that are 
            /// large address aware running on a system with 4-gigabyte tuning enabled.
            /// </summary>
            [CLSCompliant(false)]
            public ulong TotalVirtual;

            /// <summary>
            /// The amount of unreserved and uncommitted memory currently in 
            /// the user-mode portion of the virtual address space of the calling process, 
            /// in bytes.
            /// </summary>
            [CLSCompliant(false)]
            public ulong AvailableVirtual;

            /// <summary>
            /// Reserved. This value is always 0.
            /// </summary>
            [CLSCompliant(false)]
            public ulong AvailableExtendedVirtual;
        }

        #region ------------ Unmanaged calls ------------

        [DllImport("kernel32.dll")]
        private static extern void GlobalMemoryStatusEx(out MemoryStatusEx stat);

        #endregion 
       
        /// <summary>
        /// Retrieves information about the system's physical and virtual memory
        /// and its total usage.
        /// </summary>
        /// <returns>MemoryStatusEx structure contains information about the current 
        /// state of both physical and virtual memory</returns>
        public static MemoryStatusEx GetMemoryStatus()
        {
            MemoryStatusEx memEx;

            memEx.Length = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MemoryStatusEx));

            GlobalMemoryStatusEx(out memEx);

            return memEx;
            
        }
    }
}
