using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsParametersBase
{
    public class DumpCacheKeysParameters :ParameterBase
    {
        private string _cacheId = "";
        private string _keyFilter = "";
        private long _keyCount = 1000;
        private string _server = string.Empty;

        [Parameter(
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ValueFromPipeline = true,
            HelpMessage = Message.DUMP_CACHE_KEYS_CACHE)]
        [ValidateNotNullOrEmpty]
        [ArgumentAttribute("", "")]
        public string Name
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = Message.DUMP_CACHE_KEYS_KEYCOUNT)]
        [ArgumentAttribute(@"/k", @"/key-count", @"-k", @"--key-count")]
        public long KeyCount
        {
            get { return _keyCount; }
            set { _keyCount = value; }
        }

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = Message.DUMP_CACHE_KEYS_KEYFILTER)]
        [ArgumentAttribute(@"/F", @"/key-filter")]
        public string KeyFilter
        {
            get { return _keyFilter; }
            set { _keyFilter = value; }
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
