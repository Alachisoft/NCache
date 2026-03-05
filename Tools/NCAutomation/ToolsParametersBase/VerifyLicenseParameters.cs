using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsParametersBase
{

    public class VerifyLicenseParameters : PSCmdlet
    {
        public bool printLogo = true;
        private string _serverName;
        private bool _noLogo = true;
        IOutputConsole _outputProvider;
        private bool _json = false;
        int _managementPort = 8250;

        [ArgumentAttribute(@"/s", @"/server", @"-s", @"--server")]
        [Parameter(
          Mandatory = false,
          ValueFromPipelineByPropertyName = true,
          ValueFromPipeline = true,
          HelpMessage = Message.RUNNINGSERVER)]
        public string Server
        {
            get { return _serverName; }
            set
            {
                _serverName = value;

            }
        }

        [ArgumentAttribute(@"/p", @"/port", @"-p", @"--port")]
        [Parameter(
          Mandatory = false,
          ValueFromPipelineByPropertyName = true,
          ValueFromPipeline = true,
          HelpMessage = Message.PORT)]
        public int Port
        {
            get { return _managementPort; }
            set
            {
                _managementPort = value;
            }
        }



        [Argument("/?", "/help", "-?", "--help", false)]
        public SwitchParameter IsUsage { get; set; }

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = Message.NOLOGO)]
        [Argument(@"/G", @"/nologo", @"-G", @"--nologo", false)]
        public SwitchParameter NoLogo
        {
            get { return _noLogo; }
            set
            {
                _noLogo = value;
                if (_noLogo) printLogo = false;
            }
        }
        
        [Parameter(
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        ValueFromPipeline = false,
        HelpMessage = Message.JSON)]
        [ArgumentAttribute(@"/j", @"/json", @"-j", @"--json", false)]
        public SwitchParameter JSON
        {
            get { return _json; }
            set
            {
                _json = value;
                if (_json) printLogo = false;
            }
        }
        public IOutputConsole OutputProvider
        {
            set { _outputProvider = value; }
            get { return _outputProvider; }
        }
    }
}
