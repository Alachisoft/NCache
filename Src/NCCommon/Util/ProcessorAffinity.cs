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
using System.Security.Permissions;
using System.ComponentModel;

namespace Alachisoft.NCache.Common.Util
{
    /// <summary>
    /// Gets and sets the processor affinity of the current thread.
    /// </summary>
    public static class ProcessorAffinity
    {
        static class Win32Native
        {
            //GetCurrentThread() returns only a pseudo handle. No need for a SafeHandle here.
            [DllImport("kernel32.dll")]
            public static extern IntPtr GetCurrentThread();

            [HostProtectionAttribute(SelfAffectingThreading = true)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern UIntPtr SetThreadAffinityMask(IntPtr handle, UIntPtr mask);

        }

        public struct ProcessorAffinityHelper : IDisposable
        {
            UIntPtr lastaffinity;

            internal ProcessorAffinityHelper(UIntPtr lastaffinity)
            {
                this.lastaffinity = lastaffinity;
            }

            #region IDisposable Members

            public void Dispose()
            {
                if (lastaffinity != UIntPtr.Zero)
                {
                    Win32Native.SetThreadAffinityMask(Win32Native.GetCurrentThread(), lastaffinity);
                    lastaffinity = UIntPtr.Zero;
                }
            }

            #endregion
        }

        static ulong maskfromids(params int[] ids)
        {
            ulong mask = 0;
            foreach (int id in ids)
            {
                if (id < 0 || id >= Environment.ProcessorCount)
                    throw new ArgumentOutOfRangeException("CPUId", id.ToString());
                mask |= 1UL << id;
            }
            return mask;
        }

        /// <summary>
        /// Sets a processor affinity mask for the current thread.
        /// </summary>
        /// <param name="mask">A thread affinity mask where each bit set to 1 specifies a logical processor on which this thread is allowed to run. 
        /// <remarks>Note: a thread cannot specify a broader set of CPUs than those specified in the process affinity mask.</remarks> 
        /// </param>
        /// <returns>The previous affinity mask for the current thread.</returns>
        public static UIntPtr SetThreadAffinityMask(UIntPtr mask)
        {
            UIntPtr lastaffinity = Win32Native.SetThreadAffinityMask(Win32Native.GetCurrentThread(), mask);
            if (lastaffinity == UIntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return lastaffinity;
        }

        /// <summary>
        /// Sets the logical CPUs that the current thread is allowed to execute on.
        /// </summary>
        /// <param name="CPUIds">One or more logical processor identifier(s) the current thread is allowed to run on.<remarks>Note: numbering starts from 0.</remarks></param>
        /// <returns>The previous affinity mask for the current thread.</returns>
        public static UIntPtr SetThreadAffinity(params int[] CPUIds)
        {
            return SetThreadAffinityMask(((UIntPtr)maskfromids(CPUIds)));
        }

        /// <summary>
        /// Restrict a code block to run on the specified logical CPUs in conjuction with 
        /// the <code>using</code> statement.
        /// </summary>
        /// <param name="CPUIds">One or more logical processor identifier(s) the current thread is allowed to run on.<remarks>Note: numbering starts from 0.</remarks></param>
        /// <returns>A helper structure that will reset the affinity when its Dispose() method is called at the end of the using block.</returns>
        public static ProcessorAffinityHelper BeginAffinity(params int[] CPUIds)
        {
            return new ProcessorAffinityHelper(SetThreadAffinityMask(((UIntPtr)maskfromids(CPUIds))));
        }

    }    
}
