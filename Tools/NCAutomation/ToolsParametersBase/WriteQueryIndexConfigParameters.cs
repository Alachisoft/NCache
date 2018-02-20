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

using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Tools.Common;
using System.Management.Automation;

namespace Alachisoft.NCache.Automation.ToolsParametersBase
{
    public class WriteQueryIndexConfigParameters : ParameterBase
    {
        private string _AssemblyPath = string.Empty;
        private string _class = string.Empty;
        private string _outputFile = string.Empty;


        [Parameter(
         Mandatory = true,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.ASSEMBLY_PATH)]
        [ArgumentAttribute(@"/a", @"/assembly-path", @"-a", @"--assembly-path")]
        public string AssemblyPath
        {
            get { return _AssemblyPath; }
            set { _AssemblyPath = value; }
        }

        [Parameter(
         Mandatory = false,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.ADMIN_ID)]
        [ArgumentAttribute(@"/outputFile", @"/output-file", @"-a", @"--output-path")]
        public string OutputFile
        {
            get { return _outputFile; }
            set { _outputFile = value; }
        }



        [Parameter(
         Mandatory = true,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.CLASSNAME)]
        [ArgumentAttribute(@"/c", @"/class", @"-c", @"--class")]
        public string Class
        {
            get { return _class; }
            set { _class = value; }
        }

    }
}
