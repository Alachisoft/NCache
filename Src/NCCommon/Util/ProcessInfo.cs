// Copyright (c) 2017 Alachisoft
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

namespace Alachisoft.NCache.Common.Util
{

    public class ProcessInfo
    {
        public int port_number { get { return port_number; } set { port_number = value; } }
        public int pid { get { return pid; } set { pid = value; } }
        public string process_name { get { return process_name; } set { process_name = value; } }
        public string protocol { get { return protocol; } set { protocol = value; } }
    }
}
