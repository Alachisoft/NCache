using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;


namespace Alachisoft.NCache.Automation.ToolsParametersBase
{
    public class StopCacheParameters: ParameterBase
    {

        private string _server = "";
        private int _timeOut = 0;
        private string _cacheName = "";
        private int port = -1;
        private static ArrayList s_cacheId = new ArrayList();

        public bool _isTimeOutSet = false;

        public ArrayList CachesList
        {
            set { s_cacheId = value; }
            get { return s_cacheId; }
        }

        [Parameter(
           Position = 0,
           Mandatory = true,
           ValueFromPipelineByPropertyName = true,
           ValueFromPipeline = true,
           HelpMessage = Message.STARTCACHES)]
        [ValidateNotNullOrEmpty]
        public string[] Name
        {
            get; set;
        }

        [ArgumentAttribute(@"", @"")]
        public string CacheId
        {
            get { return _cacheName; }
            set
            {
                _cacheName = value;
                if (!String.IsNullOrEmpty(_cacheName) && !s_cacheId.Contains(_cacheName))
                    s_cacheId.Add(_cacheName);
            }
        }

        [ArgumentAttribute(@"/s", @"/server", @"-s", @"--server", "")]
        [Parameter(
          Mandatory = false,
          ValueFromPipelineByPropertyName = true,
          ValueFromPipeline = false,
          HelpMessage = Message.SERVER)]
        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        [ArgumentAttribute(@"/p", @"/port", @"-p", @"--port")]
        [Parameter(
          Mandatory = false,
          ValueFromPipelineByPropertyName = true,
          ValueFromPipeline = true,
          HelpMessage = Message.PORT)]
        public int Port
        {
            get { return port; }
            set { port = value; }
        }
    }
}
