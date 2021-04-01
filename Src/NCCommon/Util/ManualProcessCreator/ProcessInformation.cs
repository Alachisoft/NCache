using System;
using System.Runtime.InteropServices;

namespace Alachisoft.NCache.Common.Util
{
#if NETCORE
    [StructLayout(LayoutKind.Sequential)]
    internal class ProcessInformation
    {
        public IntPtr hProcess = IntPtr.Zero;
        public IntPtr hThread = IntPtr.Zero;
        public int dwProcessId;
        public int dwThreadId;
    }
#endif
}
