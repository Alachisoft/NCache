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
    public class ParameterBase :PSCmdlet
    {
        public bool printLogo = true;
        private bool _noLogo = true;
        IOutputConsole _outputProvider;
        internal bool isPowershell = true;



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


        public IOutputConsole OutputProvider
        {
            set { _outputProvider = value; }
            get { return _outputProvider; }
        }
    }
}
