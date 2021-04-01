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
using ComTypes = System.Runtime.InteropServices.ComTypes;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace Alachisoft.NCache.Common.Util
{
    public class CPUUsage
	{
		[DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetSystemTimes(
                    out ComTypes.FILETIME lpIdleTime,
                    out ComTypes.FILETIME lpKernelTime,
                    out ComTypes.FILETIME lpUserTime
                    );


        ComTypes.FILETIME _prevSysKernel;
        ComTypes.FILETIME _prevSysUser;
        TimeSpan _prevProcTotal;
        
        double _cpuUsage;
        DateTime _lastRun;
        long _runCount;

		public CPUUsage()
        {
            _cpuUsage = -1;
            _lastRun = DateTime.MinValue;
            _prevSysUser.dwHighDateTime = _prevSysUser.dwLowDateTime = 0;
            _prevSysKernel.dwHighDateTime = _prevSysKernel.dwLowDateTime = 0;
            _prevProcTotal = TimeSpan.MinValue;
            _runCount = 0;
        }

        public virtual double GetUsage()
        {
			double cpuCopy = _cpuUsage;
			if (Interlocked.Increment(ref _runCount) == 1)
			{
				if (!EnoughTimePassed)
				{
					Interlocked.Decrement(ref _runCount);
					return cpuCopy;
				}

				ComTypes.FILETIME sysIdle, sysKernel, sysUser;
				TimeSpan procTime;

				Process process = Process.GetCurrentProcess();

				if (process == null)
				{
					return -1;
				}

				procTime = process.TotalProcessorTime;


                if (!GetSystemTimes(out sysIdle, out sysKernel, out sysUser))
                {
                    Interlocked.Decrement(ref _runCount);
                    return cpuCopy;
                }

                if (!IsFirstRun)
                {
                    UInt64 sysKernelDiff = SubtractTimes(sysKernel, _prevSysKernel);
                    UInt64 sysUserDiff = SubtractTimes(sysUser, _prevSysUser);

                    UInt64 sysTotal = sysKernelDiff + sysUserDiff;

                    Int64 procTotal = procTime.Ticks - _prevProcTotal.Ticks;

                    if (sysTotal > 0)
                    {
                        _cpuUsage = ((100.0 * procTotal) / sysTotal);
                    }
                }

                _prevProcTotal = procTime;
                _prevSysKernel = sysKernel;
                _prevSysUser = sysUser;

                _lastRun = DateTime.Now;

                cpuCopy = _cpuUsage;
            }
            Interlocked.Decrement(ref _runCount);
            return cpuCopy;
                
        }

        private UInt64 SubtractTimes(ComTypes.FILETIME a, ComTypes.FILETIME b)
        {
            UInt64 aInt = ((UInt64)(a.dwHighDateTime << 32)) | (UInt64)a.dwLowDateTime;
            UInt64 bInt = ((UInt64)(b.dwHighDateTime << 32)) | (UInt64)b.dwLowDateTime;

            return aInt - bInt;
        }

        private bool EnoughTimePassed
        {
            get
            {
                const int minimumElapsedMS = 250;
                TimeSpan sinceLast = DateTime.Now - _lastRun;
                return sinceLast.TotalMilliseconds > minimumElapsedMS;
            }
        }

        private bool IsFirstRun
        {
            get
            {
                return (_lastRun == DateTime.MinValue);
            }
        }
	}
}
