using System;
using System.IO;
using Alachisoft.NCache.Common;
namespace Alachisoft.NGroups
{
    internal class Version
    {
        public static byte[] version_id;
        private static bool initialized;

        public static void Initialize(string versionType)
        {
            if (initialized) return;

            if (!string.IsNullOrEmpty(versionType))
                if (versionType.Equals(VersionType.Express.ToString().ToLower()))
                    version_id = new byte[] { (byte)'N', (byte)'C', (byte)'P', (byte)'X', 5, 0 }; //Express
                else if (versionType.Equals(VersionType.OpenSource.ToString().ToLower()))
                    version_id = new byte[] { (byte)'N', (byte)'C', (byte)'O', (byte)'S', (byte)'S',5, 0 }; //OpenSource
                else if (versionType.Equals(VersionType.Cloud.ToString().ToLower()))
                    version_id = new byte[] { (byte)'N', (byte)'C', (byte)'C', (byte)'D', 5, 0 }; //Cloud
                else if (versionType.Equals(VersionType.Dev.ToString().ToLower()))
                    version_id = new byte[] { (byte)'N', (byte)'C', (byte)'D', (byte)'V', 5, 0 }; //Dev
                else if (versionType.Equals(VersionType.ServerOnly.ToString().ToLower()))
                    version_id = new byte[] { (byte)'N', (byte)'C', (byte)'S', (byte)'O', 5, 0 }; //Server Only licensing
                else if (versionType.Equals(VersionType.InEvaluation.ToString().ToLower()))
                    version_id = new byte[] { (byte)'N', (byte)'C', (byte)'P', (byte)'V', 5, 0 }; //In Evaluation
            initialized = true;
        }

        public static bool IsExpress(string versionType)
        {
            if (versionType.Equals(VersionType.Express.ToString().ToLower()))
                return true;
            else
                return false;
        }


        public static string printVersionId(byte[] v)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (v != null)
            {
                int len = Length;
                if (v.Length < len) len = v.Length;

                for (int i = 0; i < len; i++)
                    sb.Append((char)v[i]);
            }
            return sb.ToString();
        }

        public static int Length { get { return 4; } }

        public static bool CompareTo(byte[] v)
        {
            if (v == null || v.Length < version_id.Length)
            {
                return false;
            }
            if (version_id[2] == (byte)'P' && version_id[3] == (byte)'V' && v[2] == (byte)'P' && v[3] == (byte)'N')
            {
                if (version_id[4] == v[4] && version_id[5] == v[5])
                    return true;
            }
            if (version_id[2] == (byte)'P' && version_id[3] == (byte)'N' && v[2] == (byte)'P' && v[3] == (byte)'V')
            {
                if (version_id[4] == v[4] && version_id[5] == v[5])
                    return true;
            }
            if (version_id[2] == (byte)'P' && version_id[3] == (byte)'V' && v[2] == (byte)'S' && v[3] == (byte)'O')
            {
                if (version_id[4] == v[4] && version_id[5] == v[5])
                    return true;
            }
            if (version_id[2] == (byte)'S' && version_id[3] == (byte)'O' && v[2] == (byte)'P' && v[3] == (byte)'V')
            {
                if (version_id[4] == v[4] && version_id[5] == v[5])
                    return true;
            }
            else if (version_id[0] == v[0] && version_id[1] == v[1] && version_id[2] == v[2] && version_id[3] == v[3] && version_id[4] == v[4] && version_id[5] == v[5])
            {
                return true;
            }

            return false;
        }
    }
}