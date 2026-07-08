using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsParametersBase
{
    public class ListCachesParameters :ParameterBase
    {

        private string _server = string.Empty;
        private int _port = -1;
        private bool _detailed;
        private StoreType? _storeType = null;
        CacheTopologyParam? _topology = null;


        [Parameter(
           ValueFromPipeline = true,
           HelpMessage = Message.SERVER)]
        [ArgumentAttribute(@"/s", @"/server", @"-s", @"--server")]
        public string Server { get { return _server; } set { _server = value; } }

        [Parameter(
         Mandatory = false,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = true,
         HelpMessage = Message.STORETYPE)]
        [ArgumentAttribute(@"/e", @"/storetype", @"-e", @"--storetype")]
        public StoreType? InMemoryStoreType
        {
            get { return _storeType; }
            set { _storeType = value; }
        }

        [Parameter(
          Mandatory = false,
          ValueFromPipelineByPropertyName = true,
          ValueFromPipeline = true,
          HelpMessage = Message.TOPOLOGY)]
        [ArgumentAttribute(@"/t", @"/topology", @"-t", @"--topology")]
        [ValidateNotNullOrEmpty]
        public CacheTopologyParam? Topology
        {
            get { return _topology; }
            set { _topology = value; }
        }

        [Parameter(
        ValueFromPipelineByPropertyName  = true,
        HelpMessage = Message.PORT)]
        [ArgumentAttribute(@"/p", @"/port", @"-p", @"--port")]
        public int Port { get { return _port; } set { _port = value; } }


        [Parameter(
        ValueFromPipelineByPropertyName = true,
        HelpMessage = Message.LIST_CACHES_DETAILS)]
        [ArgumentAttribute(@"/a", @"/detail", @"-a", @"--detail", false)]
        public SwitchParameter Detail { get { return _detailed; } set { _detailed = value; } }

      
    }
}
