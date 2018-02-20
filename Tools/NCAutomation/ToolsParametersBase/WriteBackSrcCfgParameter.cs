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
    public class WriteBackSrcCfgParameter : ParameterBase
    {
        private string _assemblyPath = string.Empty;
        private string _class = string.Empty;
        private string _parameters = string.Empty;
        private bool _readThru = false;
        private bool _writeThru = false;
        private string _providerName = string.Empty;
        private bool _isdefault = false;
        private string _outputFile = string.Empty;


        [Parameter(
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        ValueFromPipeline = false,
        HelpMessage = Message.ASSEMBLY_PATH)]
        [ArgumentAttribute(@"/path", @"/assembly-path", @"-path", @"--assembly-path")]
        public string AssemblyPath
        {
            get { return _assemblyPath; }
            set { _assemblyPath = value; }
        }

        [Parameter(
       Mandatory = false,
       ValueFromPipelineByPropertyName = true,
       ValueFromPipeline = false,
       HelpMessage = Message.OUTPUTFILE)]
        [ArgumentAttribute(@"/path", @"/assembly-path", @"-path", @"--assembly-path")]
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

        [Parameter(
         Mandatory = false,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.PARAMETERSLIST)]
        [ArgumentAttribute(@"/pl", @"/parameter-list", @"-pl", @"--parameter-list")]
        public string Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        [Parameter(
         Mandatory = false,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.READTHRU)]
        [ArgumentAttribute(@"/R", @"/readthru", @"-R", @"--readthru", false)]
        public SwitchParameter ReadThru
        {
            get { return _readThru; }
            set { _readThru = value; }
        }

        [Parameter(
         Mandatory = false,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.WRITETHRU)]
        [ArgumentAttribute(@"/W", @"/writethru", @"-W", @"--writethru", false)]
        public SwitchParameter WriteThru
        {
            get { return _writeThru; }
            set { _writeThru = value; }
        }

        [Parameter(
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        ValueFromPipeline = true,
        HelpMessage = Message.PROVIDERNAME)]
        [ArgumentAttribute(@"/pn", @"/provider-name", @"-pn", @"--provider-name")]
        public string ProviderName
        {
            get { return _providerName; }
            set { _providerName = value; }
        }

        [Parameter(
         Mandatory = false,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = true,
         HelpMessage = Message.DEFAULT)]
        [ArgumentAttribute(@"/d", @"/default", @"-d", @"--default", false)]
        public SwitchParameter DefaultProvider
        {
            get { return _isdefault; }
            set { _isdefault = value; }
        }
    }
}
