using System;
using System.IO;
using Alachisoft.NCache.Common;
namespace Alachisoft.NGroups
{
    internal class Version
    {
        public static byte[] version_id;
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized) return;
            version_id = new byte[] { (byte)'N', (byte)'C', (byte)'O', (byte)'S', (byte)'S', 5, 0 };

            initialized = true;
        }
  
        public static int Length { get { return 4; } }

        public static bool CompareTo(byte[] v)
        {
            if (v == null || v.Length < version_id.Length)
            {
                return false;
            }
            if(version_id[0] == v[0] && version_id[1] == v[1] && version_id[2] == v[2] && version_id[3] == v[3] && version_id[4] == v[4] && version_id[5] == v[5])
            {
                return true;
            }

            return false;
        }
    }
}