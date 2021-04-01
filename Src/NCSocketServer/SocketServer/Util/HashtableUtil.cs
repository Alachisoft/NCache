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

using System;
using System.Collections;
using System.Text;

namespace Alachisoft.NCache.SocketServer.Util
{
    /// <summary>
    /// Provide methods to convert hashtable into a string form, and repopulating
    /// hashtable from string. The conversion do not save type information and assumes
    /// that keys are of int type, while values are of string type
    /// </summary>
    public static class HashtableUtil
    {
        /// <summary>
        /// Convert hashtable to a string form
        /// </summary>
        /// <param name="table">Hashtable containing int key and string value</param>
        /// <returns>String representation of hashtable</returns>
        public static string ToString(Hashtable table)
        {
            if (table != null)
            {
                StringBuilder toStr = new StringBuilder();
                foreach (DictionaryEntry entry in table)
                {
                    toStr.AppendFormat("{0}${1}\r\n", entry.Key, entry.Value);
                }
                return toStr.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string ToString(ArrayList list)
        {
            if (list != null)
            {
                StringBuilder toStr = new StringBuilder();
                foreach (Object entry in list)
                {
                    toStr.AppendFormat("{0}\r\n", entry.ToString());
                }
                return toStr.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Populate a hashtable from its string representation
        /// </summary>
        /// <param name="rep">String representation of hashtable</param>
        /// <returns>Hashtable formed from string representation</returns>
        public static Hashtable FromString(string rep)
        {
            if (rep != null && rep != string.Empty)
            {
                Hashtable table = new Hashtable();
                string[] entries = rep.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string entry in entries)
                {
                    string[] keyVal = entry.Split('$');
                    table.Add(int.Parse(keyVal[0]), keyVal[1]);
                }
                return table;
            }
            return null;
        }
    }
}
