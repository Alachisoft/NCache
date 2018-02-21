// Copyright (c) 2018 Alachisoft
// 
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
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.Common.Caching
{
    public class CacheKeyGenerator
    {
        public string Suffix { get; set; }
        public string Prefix { get; set; }
        public int BlockID { get; set; }
        public string ID { get; private set; }

        public CacheKeyGenerator(string id)
        {
            this.ID = id;
        }

        public string Current
        {
            get
            {
                string key = GenerateKey(BlockID.ToString());
                return key;
            }
        }

        public string FirstKey
        {
            get
            {
                string key = GenerateKey("0");
                return key;
            }
        }
        public string EofKey
        {
            get
            {
                string key = GenerateKey("EOF");
                return key;
            }
        }

        public string GroupName
        {
            get
            {
                string key = GenerateKey("X");
                return key;
            }
        }

        public string LockId
        {
            get
            {
                string key = GenerateKey("L");
                return key;
            }
        }

        public string GetNextKey()
        {
            string key = Current;
            BlockID++;
            return key;
        }

        public string GenerateKey(string block)
        {
            string format = "{0}:{1}:{2}:{3}";
            string key = String.Format(format, Prefix, ID, block, Suffix);
            return key;
        }
    }
}
