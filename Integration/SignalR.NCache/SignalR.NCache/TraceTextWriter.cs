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

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Alachisoft.NCache.SignalR
{
    internal class TraceTextWriter : TextWriter
    {
        private readonly string _prefix;
        private readonly TraceSource _trace;

        public TraceTextWriter(string prefix, TraceSource trace) : base(CultureInfo.CurrentCulture)
        {
            _prefix = prefix;
            _trace = trace;
        }

        public override Encoding Encoding
        {
            get{return Encoding.UTF8;}
        }

        public override void Write(char value)
        {

        }

        public override void WriteLine(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _trace.TraceVerbose(_prefix + value);
            }
        }
    }
}