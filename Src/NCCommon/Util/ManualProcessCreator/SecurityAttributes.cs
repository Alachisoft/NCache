using System;
using System.Runtime.InteropServices;

namespace Alachisoft.NCache.Common.Util
{
#if NETCORE
    [StructLayout(LayoutKind.Sequential)]
    internal class SecurityAttributes
    {
        public int nLength = 12;
        public SafeLocalMemHandle lpSecurityDescriptor = new SafeLocalMemHandle(IntPtr.Zero, false);
        public bool bInheritHandle;
    }
#endif
}
