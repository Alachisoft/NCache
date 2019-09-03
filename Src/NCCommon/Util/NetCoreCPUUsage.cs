using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Alachisoft.NCache.Common.Util
{
    public class NetCoreCPUUsage: CPUUsage
    {
        Process _thisProcess;
        TimeSpan _startProcessorTime;
        TimeSpan _oldCPUTime = new TimeSpan(0);
        DateTime _lastMonitorTime = DateTime.UtcNow;
   
        public NetCoreCPUUsage()
        {
            _thisProcess = Process.GetCurrentProcess();
            _startProcessorTime = _thisProcess.TotalProcessorTime;
            
        }

        // Call this every 30 seconds
        public  double CalculateCPUUSage()
        {
            TimeSpan newCPUTime = _thisProcess.TotalProcessorTime - _startProcessorTime;
            var cpuUsage = (newCPUTime - _oldCPUTime).TotalSeconds / (Environment.ProcessorCount * DateTime.UtcNow.Subtract(_lastMonitorTime).TotalSeconds);
            _lastMonitorTime = DateTime.UtcNow;
            _oldCPUTime = newCPUTime;
            return cpuUsage;
        }

        public override double GetUsage()
        {
            return CalculateCPUUSage() *100;
        }

    }
}
