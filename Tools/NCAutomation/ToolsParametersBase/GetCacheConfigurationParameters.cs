using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsParametersBase
{
    public class GetCacheConfigurationParameters : ParameterBase
    {
        string _path = string.Empty;
        string _file = string.Empty;
        private string _cacheId = string.Empty;
        private string _server = string.Empty;
        private int _port = -1;

        [Parameter(
         Mandatory = true,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.GENERATE_CONFIG_PATH)]
        [ArgumentAttribute(@"/T", @"/path", @"-T", @"--path")]
        [ValidateNotNullOrEmpty]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        [Parameter(
         Position = 0,
         Mandatory = false,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.GET_CACHE_CONFIGURATION)]
        [ValidateNotNullOrEmpty]
        [ArgumentAttribute("", "")]
        public string Name
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        [Parameter(
         Mandatory = false,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.SERVER)]
        [ArgumentAttribute(@"/s", @"/server", @"-s", @"--server")]
        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }


       [Parameter(
       Mandatory = false,
       ValueFromPipelineByPropertyName = true,
       ValueFromPipeline = false,
       HelpMessage = Message.PORT)]
        [ArgumentAttribute(@"/p", @"/port", @"-p", @"--port")]
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }
    }
}
