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
namespace Alachisoft.NCache.Common.DataStructures
{
    /// <summary>
    /// Enumeration that defines the status of the bucket.
    /// </summary>
    public class BucketStatus
    {
        /// <summary> The bucket is yet to be state transferred from the source node. </summary>
        public const byte NeedTransfer = 1;

        /// <summary> The bucket is being transfered from the source node to some target node.</summary>
        public const byte UnderStateTxfr = 2;

        /// <summary> The bucket is fully functional. </summary>
        public const byte Functional = 4;

        public static string StatusToString(byte status)
        {
            string toString = null;
            switch (status)
            {
                case NeedTransfer:
                    toString = "NeedTransfer";
                    break;

                case UnderStateTxfr:
                    toString = "UnderStateTxfr";
                    break;

                case Functional:
                    toString = "Functional";
                    break;

                default:
                    toString = "UNDEFINED";
                    break;
            }
            return toString;
        }
    }
}