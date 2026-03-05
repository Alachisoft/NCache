using System;

namespace Lextm.SharpSnmpLib.Browser
{
    internal static class VersionCodeExtension
    {
        public static int ToSelectedIndex(this VersionCode version)
        {
            switch (version)
            {
                case VersionCode.V1:
                    return 0;
                case VersionCode.V2:
                    return 1;
                case VersionCode.V3:
                    return 2;
                default:
                    throw new ArgumentException(@"unknown version", "version");
            }
        }

        public static VersionCode FromSelectedIndex(int selected)
        {
            switch (selected)
            {
                case 0:
                    return VersionCode.V1;
                case 1:
                    return VersionCode.V2;
                case 2:
                    return VersionCode.V3;
                default:
                    throw new ArgumentException(@"unknown selection index", "selected");
            }
        }
    }
}
