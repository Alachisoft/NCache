using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsParametersBase
{
    public class ClearCacheParameters : ParameterBase
    {
        private string s_cacheId = "";
        private bool s_clearJsCss = false;
        private bool s_forceClear = false;
        private string _server = string.Empty;

        [Parameter(
         Position = 0,
         Mandatory = true,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.CACHENAME)]
        [ArgumentAttribute(@"", @"")]
        public string Name
        {
            get { return s_cacheId; }
            set { s_cacheId = value; }
        }

        [Parameter(
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.FORCECLEAR)]
        [ArgumentAttribute(@"/F", @"/force", @"-F", @"--force", false)]
        public SwitchParameter Force
        {
            get { return s_forceClear; }
            set { s_forceClear = value; }
        }

        [Parameter(
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        ValueFromPipeline = false,
        HelpMessage = Message.SERVERS)]
        [ArgumentAttribute(@"/s", @"/server", @"-s", @"--server")]
        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

    }
}
