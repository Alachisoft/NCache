// Parts based on code by MJ Hutchinson http://mjhutchinson.com/journal/2010/01/25/integrating_gtk_application_mac

using System;
using System.Runtime.InteropServices;

namespace RemObjects.Mono.Helpers
{
    public enum PlatformType
    {
        Windows,
        WinCE,
        XBox,
        Mac,
        Linux,
        UnixUnknown,
        Unknown
    }

    public static class PlatformSupport
    {
        static readonly PlatformType platform;
        static readonly String unameResult;

        [DllImport("libc")]
        extern static int uname(IntPtr buf);
        static string intuname()
        {
            IntPtr buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(8192);
                // This is a hacktastic way of getting sysname from uname ()
                if (uname(buf) == 0)
                {
                    return Marshal.PtrToStringAnsi(buf);
                }
            }
            finally
            {
                if (buf != IntPtr.Zero)
                    Marshal.FreeHGlobal(buf);
            }
            return null;
        }

        static PlatformSupport()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.WinCE: platform = PlatformType.WinCE; break;
                case PlatformID.Win32NT: platform = PlatformType.Windows; break;
                case PlatformID.Win32S: platform = PlatformType.Windows; break;
                case PlatformID.Win32Windows: platform = PlatformType.Windows; break;
                case PlatformID.Xbox: platform = PlatformType.XBox; break;
                default:
                    unameResult = intuname();
                    if (unameResult == null)
                        platform = PlatformType.Unknown;
                    else if (unameResult == "Linux")
                        platform = PlatformType.Linux;
                    else if (unameResult == "Darwin")
                        platform = PlatformType.Mac;
                    else
                        platform = PlatformType.UnixUnknown;
                    break;
            }
        }

        public static PlatformType Platform
        {
            get
            {
                return platform;
            }
        }

        public static string UNameResult
        {
            get
            {
                return unameResult;
            }
        }
    }
}