// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
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
            version_id = new byte[] { (byte)'N', (byte)'C', (byte)'O', (byte)'S', (byte)'S', 4, 9 };

            initialized = true;

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
            if (version_id[2] == (byte)'E' && version_id[3] == (byte)'V' && v[2] == (byte)'E' && v[3] == (byte)'N')
            {
                if (version_id[4] == v[4] && version_id[5] == v[5])
                    return true;
            }
            if (version_id[2] == (byte)'E' && version_id[3] == (byte)'N' && v[2] == (byte)'E' && v[3] == (byte)'V')
            {
                if (version_id[4] == v[4] && version_id[5] == v[5])
                    return true;
            }
            if (version_id[2] == (byte)'E' && version_id[3] == (byte)'V' && v[2] == (byte)'S' && v[3] == (byte)'O')
            {
                if (version_id[4] == v[4] && version_id[5] == v[5])
                    return true;
            }
            if (version_id[2] == (byte)'S' && version_id[3] == (byte)'O' && v[2] == (byte)'E' && v[3] == (byte)'V')
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