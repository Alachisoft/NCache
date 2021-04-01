//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Collections;
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class Log: ICloneable,ICompactSerializable
    {

        bool enabled=true;
        bool traceErrors = true; 
        bool traceWarnings, traceNotices, traceDebug;
        String location = "";
        public Log() { }

        [ConfigurationAttribute("enable-logs")]//Changes for New Dom from enabled
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        [ConfigurationAttribute("trace-errors")]
        public bool TraceErrors
        {
            get { return traceErrors; }
            set { traceErrors = value; }
        }

        [ConfigurationAttribute("trace-notices")]
        public bool TraceNotices
        {
            get { return traceNotices; }
            set { traceNotices = value; }
        }

        [ConfigurationAttribute("trace-warnings")]
        public bool TraceWarnings
        {
            get { return traceWarnings; }
            set { traceWarnings = value; }
        }

        [ConfigurationAttribute("trace-debug")]
        public bool TraceDebug
        {
            get { return traceDebug; }
            set { traceDebug = value; }
        }

        [ConfigurationAttribute("log-path")]
        public String LogPath
        {
            get
            {
                if (location == null)
                    return string.Empty;
                else 
                    return location;
            }
            set { location = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            Log log = new Log();
            log.Enabled = Enabled;
            log.TraceDebug = TraceDebug;
            log.TraceErrors = TraceErrors;
            log.TraceNotices = TraceNotices;
            log.TraceWarnings = TraceWarnings;
            log.LogPath = LogPath;
            return log;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            enabled = reader.ReadBoolean();
            traceErrors = reader.ReadBoolean();
            traceWarnings = reader.ReadBoolean();
            traceNotices = reader.ReadBoolean();
            traceDebug = reader.ReadBoolean();
            location = (string)reader.ReadObject();
  
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(enabled);
            writer.Write(traceErrors);
            writer.Write(traceWarnings);
            writer.Write(traceNotices);
            writer.Write(traceDebug);
            writer.WriteObject(location);
           
        }

        #endregion
    }
}
