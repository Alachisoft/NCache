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
using System.Collections.Generic;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using System.Net;

namespace Alachisoft.NCache.Common.Snmp
{
    public class SnmpMessenger : IDisposable
    {
#if !NETCORE
        private Messenger messenger;
#endif
        string nodeName;
        public int port;
        public int timeout;
        private bool exceptionOccurred;


        public Boolean ExceptionOccurred
        {
            get
            {
                return exceptionOccurred;
            }
        }

        public SnmpMessenger(string nodeName, int port) : this(nodeName, port, 1000)
        {
        }

        public SnmpMessenger(string nodeName, int port, int timeout)
        {
            this.nodeName = nodeName;
            this.port = port;
            this.timeout = timeout;
            this.InitializeMessenger();
        }

        public void InitializeMessenger()
        {
            IPAddress ip;
            IPAddress.TryParse(nodeName, out ip);
        }

        public IList<CountersViewModel> GetValuesAgainstOids(IList<string> strOids)
        {
            return GetValuesAgainstOids(strOids, "public");
        }

        public IList<CountersViewModel> GetValuesAgainstOids(IList<string> strOids, String community)
        {

            IList<Variable> oids = new List<Variable>();
            foreach (string oid in strOids)
                oids.Add(new Variable(new ObjectIdentifier(String.Format(".{0}.0", oid))));
            try
            {
#if !NETCORE
                if (this.messenger != null)
                {
                    oids = messenger.GetValuesAgaintOIDs(new OctetString(community), oids);
                    exceptionOccurred = false;
                }
#endif
            }
            catch (Exception ex)
            {
                exceptionOccurred = true;
                throw;
            }
            List<Alachisoft.NCache.Common.Snmp.CountersViewModel> countersVM = new List<Alachisoft.NCache.Common.Snmp.CountersViewModel>();
            foreach (Variable v in oids)
                countersVM.Add(new CountersViewModel(v));
            return countersVM;

        }
        
        
        public object GetValuesAgainstOids(string oid)
        {
            return GetValuesAgainstOids(oid, "public");
        }

        public object GetValuesAgainstOids(string oid, String community)
        {

            IList<Variable> oids = new List<Variable>();

            oids.Add(new Variable(new ObjectIdentifier(String.Format(".{0}.0", oid))));

            try
            {
#if !NETCORE
                if (this.messenger != null)
                {
                    oids = messenger.GetValuesAgaintOIDs(new OctetString(community), oids);
                    exceptionOccurred = false;
                }
#endif
            }
            catch (Exception ex)
            {
                exceptionOccurred = true;
                throw;
            }

            if (oids != null && oids.Count > 0)
            {
                ISnmpData data = oids[0].Data;
                if (data != null)
                {
                    string dataStr = data.ToString();

                    if (!data.ToString().Equals("Null"))
                        return dataStr;

                }
            }

            return null;
        }

        public void Dispose()
        {
#if !NETCORE
            if (messenger != null)
                messenger.Dispose();
            messenger = null;
#endif
        }

    }
}
