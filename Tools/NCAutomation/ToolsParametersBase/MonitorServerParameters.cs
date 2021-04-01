using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsParametersBase
{
    public class MonitorServerParameters :ParameterBase
    {
        private string _action = "";
        private string _server = "";
        private int _port = -1;

        [Parameter(
           Mandatory = true,
           ValueFromPipelineByPropertyName = true,
           ValueFromPipeline = true,
           HelpMessage = Message.MONITOR_SERVER_ACTION)]
        [ValidateNotNullOrEmpty]
        [ArgumentAttribute("", "")]
        public string Action
        {
            get { return _action; }
            set { _action = value; }
        }

        [Parameter(
           ValueFromPipelineByPropertyName = true,
           HelpMessage = Message.SERVER)]
        [ArgumentAttribute(@"/s", @"/server", @"-s", @"--server")]
        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        [Parameter(
           ValueFromPipelineByPropertyName = true,
           HelpMessage = Message.PORT)]
        [ArgumentAttribute(@"/p", @"/port", @"-p", @"--port")]
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

    }
}
