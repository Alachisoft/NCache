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
using Alachisoft.NCache.Common.Communication;

namespace Alachisoft.NCache.Management.RPC
{
    public class ConsoleTraceProvider : ITraceProvider
    {

        public void TraceCritical(string module, string message)
        {
            WriteTraceToConsole("Critical", module, message);
        }

        public void TraceError(string module, string errorMessage)
        {
            WriteTraceToConsole("Error", module, errorMessage);
        }

        public void TraceWarning(string module, string warningMessage)
        {
            WriteTraceToConsole("Warning", module, warningMessage);
        }

        public void TraceDebug(string module, string debug)
        {
            WriteTraceToConsole("Debug", module, debug);
        }

        private void WriteTraceToConsole(string traceLevel, string module, string message)
        {
            string finalStr = "[ConsoleTrace] " + DateTime.Now.ToString() + "      [" + traceLevel + "]       [" + module + "]      " + message;
            lock (this)
            {
                Console.WriteLine(finalStr);
            }
        }
    }
}