//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
namespace Alachisoft.NCache.Caching
{
    public class NCHeader
    {
        public static byte[] version_id = new byte[] { (byte)'N', (byte)'C', 2, 1, 0 };

        static NCHeader()
        {
            version_id[4] = (byte)(version_id[0] | version_id[1] | version_id[2] | version_id[3]);
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


        public static int Length { get { return 5; } }

        public static bool CompareTo(byte[] v)
        {
            if (v == null || v.Length < version_id.Length)
                return false;

            return version_id[0] == v[0] &&
                   version_id[1] == v[1] &&
                   version_id[2] == v[2] &&
                   version_id[3] == v[3] &&
                   version_id[4] == v[4];
        }
        public static byte[] Version
        {
            get { return version_id; }
        }
    }
}