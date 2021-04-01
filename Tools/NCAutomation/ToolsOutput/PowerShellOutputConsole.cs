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
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Alachisoft.NCache.Automation.Util;

namespace Alachisoft.NCache.Automation.ToolsOutput
{
    public class PowerShellOutputConsole : PSCmdlet, IOutputConsole
    {
        PSCmdlet _cmdLet;


        private ErrorRecord GetErrorRecord(string message)
        {
            return new ErrorRecord(new Exception(message), "Tools", ErrorCategory.NotSpecified, null);
        }

        public PowerShellOutputConsole(PSCmdlet cmdLet)
        {
            _cmdLet = cmdLet;
        }

        public void WriteErrorLine(string message)
        {
            _cmdLet.WriteError(GetErrorRecord(message));
        }

        public void WriteErrorLine(string format, object arg0, object arg1, object arg2)
        {
            string fomratedString = string.Format(format, arg0, arg1, arg2);
            _cmdLet.WriteError(GetErrorRecord(fomratedString));
        }

        public void WriteErrorLine(string format, object arg0, object arg1, object arg2, object arg3)
        {
            string fomratedString = string.Format(format, arg0, arg1, arg2, arg3);
            _cmdLet.WriteError(GetErrorRecord(fomratedString));
        }

        public void WriteErrorLine(string format, object arg0, object arg1)
        {
            string fomratedString = string.Format(format, arg0, arg1);
            _cmdLet.WriteError(GetErrorRecord(fomratedString));
        }

        public void WriteErrorLine(string format, string message)
        {
            string fomratedString = string.Format(format, message);
            _cmdLet.WriteError(GetErrorRecord(fomratedString));
        }

        public void WriteErrorLine(object message)
        {
            _cmdLet.WriteError(GetErrorRecord(message.ToString()));
        }

        public void WriteLine(object message)
        {
            _cmdLet.WriteObject(new string[] { message.ToString() });
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            string fomratedString = string.Format(format, arg0, arg1, arg2);
            _cmdLet.WriteObject(new string[] { fomratedString });
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3)
        {
            string fomratedString = string.Format(format, arg0, arg1, arg2, arg3);
            _cmdLet.WriteObject(new string[] { fomratedString });
        }

        public void WriteLine(string format, object arg0, object arg1)
        {
            string fomratedString = string.Format(format, arg0, arg1);
            _cmdLet.WriteObject(new string[] { fomratedString });
        }

        public void WriteLine(string format, string message)
        {
            string fomratedString = string.Format(format, message);
            _cmdLet.WriteObject(new string[] { fomratedString });
        }

        public void WriteLine(string message)
        {
            _cmdLet.WriteObject(new string[] { message });
        }

        public void WriteLine(string message, object args0)
        {
            _cmdLet.WriteObject(new string[] { message, args0.ToString() });
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
        {
            string fomratedString = string.Format(format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            _cmdLet.WriteObject(new string[] { fomratedString });
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            string fomratedString = string.Format(format, arg0, arg1, arg2, arg3, arg4);
            _cmdLet.WriteObject(new string[] { fomratedString });
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            string fomratedString = string.Format(format, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
            _cmdLet.WriteObject(new string[] { fomratedString });
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            string fomratedString = string.Format(format, arg0, arg1, arg2, arg3, arg4, arg5);
            _cmdLet.WriteObject(new string[] { fomratedString });
        }
        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
        {
            string fomratedString = string.Format(format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            _cmdLet.WriteObject(new string[] { fomratedString });
        }
    }
}
