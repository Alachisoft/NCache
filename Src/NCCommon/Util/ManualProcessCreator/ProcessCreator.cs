using System;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Alachisoft.NCache.Common.Util
{
    internal static class ProcessCreator
    {
        private const int normalPriorityClass = 0x0020;

        [DllImport("Kernel32", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        internal static extern bool CreateProcess(
             [MarshalAs(UnmanagedType.LPTStr)]string applicationName,
             StringBuilder commandLine,
             SecurityAttributes processAttributes,
             SecurityAttributes threadAttributes,
             bool inheritHandles,
             int creationFlags,
             IntPtr environment,
             [MarshalAs(UnmanagedType.LPTStr)]string currentDirectory,
             StartupInfo startupInfo,
             ProcessInformation processInformation
        );

        internal static Process CreateProcess(string executable, string arguments)
        {
            var startupInfo = new StartupInfo();
            var threadSecurity = new SecurityAttributes();
            var processSecurity = new SecurityAttributes();
            var processInformation = new ProcessInformation();

            processSecurity.nLength = Marshal.SizeOf(processSecurity);
            threadSecurity.nLength = Marshal.SizeOf(threadSecurity);

            // We can use the string builder to build up our full command line, including arguments
            var commandLine = new StringBuilder();
            commandLine.Append(executable)
                       .Append(' ')
                       .Append(arguments);

            if (CreateProcess(null, commandLine, processSecurity, threadSecurity, false, normalPriorityClass, IntPtr.Zero, null, startupInfo, processInformation))
            {
                // Process was created successfully
                var safeProcessHandle = new SafeProcessHandle(processInformation.hProcess, true);

                if (!safeProcessHandle.IsInvalid)
                {
                    var process = Process.GetProcessById(processInformation.dwProcessId);
                    safeProcessHandle.Close();
                    return process;
                }
                // Not sure what to do here but hopefully this wouldn't arise.
                // There should be some form of exception thrown here to ensure 
                // the above layer of this case but is it really necessary? Since 
                // the process HAS started anyway. I should consult Sir Taimoor 
                // on this.
            }

            // We couldn't create the process, so raise an exception with the details.
            throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
    }
}
