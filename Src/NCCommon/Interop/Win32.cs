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
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Collections;
using System.Reflection;
//using System.Runtime.Remoting;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;


namespace Alachisoft.NCache.Common.Interop
{
    /// <summary>
	/// Utility class to help with interop tasks.
	/// </summary>
	[CLSCompliant(false)]
	public class Win32
	{
		private Win32() { }

        [DllImport("kernel32")]
        public static extern bool QueryPerformanceFrequency(ref long frequency);

		[DllImport("kernel32")]
		public static extern void QueryPerformanceCounter(ref long ticks);

		[DllImport("kernel32")]
		public static extern void GetSystemInfo(ref SYSTEM_INFO pSI);
        
        [DllImport("kernel32")]
        public static extern void GetNativeSystemInfo(ref SYSTEM_INFO pSI);        

		[DllImport("kernel32")]
		public static extern uint GetLastError();

		[DllImport("kernel32")]
		public static extern uint FormatMessage(
			uint dwFlags, // Source and processing options
			IntPtr lpSource, // Message source
			uint dwMessageId, // Message identifier
			uint dwLanguageId, // Language identifier
			StringBuilder lpBuffer, // Message buffer
			uint nSize, // Maximum size of message buffer
			IntPtr Arguments  // Array of message inserts
			);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        public static bool InternalCheckIsWow64()
        {          
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6)
            {
                using (Process p = Process.GetCurrentProcess())
                {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal))
                    {
                        return false;
                    }
                    return retVal;
                }
            }
            else
            {
                return false;
            }
        }
    }



}
