using System;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Alachisoft.NCache.Common.Util
{
#if NETCORE
    [StructLayout(LayoutKind.Sequential)]
    internal class StartupInfo
    {
        public int cb;
        public IntPtr lpReserved = IntPtr.Zero;
        public IntPtr lpDesktop = IntPtr.Zero;
        public IntPtr lpTitle = IntPtr.Zero;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2 = IntPtr.Zero;
        public SafeFileHandle hStdInput = new SafeFileHandle(IntPtr.Zero, false);
        public SafeFileHandle hStdOutput = new SafeFileHandle(IntPtr.Zero, false);
        public SafeFileHandle hStdError = new SafeFileHandle(IntPtr.Zero, false);

        public StartupInfo()
        {
            dwY = -1;
            cb = Marshal.SizeOf(this);
        }

        public void Dispose()
        {
            // close the handles created for child process
            if (hStdInput != null && !hStdInput.IsInvalid)
            {
                hStdInput.Close();
                hStdInput = null;
            }

            if (hStdOutput != null && !hStdOutput.IsInvalid)
            {
                hStdOutput.Close();
                hStdOutput = null;
            }

            if (hStdError == null || hStdError.IsInvalid) return;

            hStdError.Close();
            hStdError = null;
        }
    }
#endif
}
