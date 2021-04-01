using Alachisoft.NCache.Runtime.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.SocketServer
{
    internal class ClientLedger
    {
        private IDictionary<IPAddress, IList<string>> _cacheRegisteredClients;
        private static ClientLedger _cacheClientLicenseManager = null;
        private object _mutex = new object();
        private static object _creationMutex = new object();

        internal ClientLedger()
        {
            _cacheRegisteredClients = new Dictionary<IPAddress, IList<string>>();
        }

        public void RegisterClientForCache(IPAddress address, string clientId)
        {
            if (address != null && clientId != null)
            {
                lock (_mutex)
                {
                    if (_cacheRegisteredClients.ContainsKey(address))
                    {
                        IList<string> list = _cacheRegisteredClients[address] as List<string>;
                        if (list != null)
                            list.Add(clientId);
                    }
                    else
                    {
                        if (_cacheRegisteredClients.Count >= 2)
                            throw new LicensingException("NCache Open Source edition does not support more than 2 clients at a time.");

                        _cacheRegisteredClients.Add(address, new List<string>() { clientId });
                    }                   
                } 
            }
        }

        public void UnregisterClientForCache(IPAddress address, string clientId)
        {
            if (address != null && clientId != null)
            {
                lock (_mutex)
                {
                    if (_cacheRegisteredClients.ContainsKey(address))
                    {
                        IList<string> list = _cacheRegisteredClients[address] as List<string>;
                        if (list != null)
                            list.Remove(clientId);

                        if (list == null || list.Count == 0)
                            _cacheRegisteredClients.Remove(address);

                    }
                } 
            }
        }
        
        public static ClientLedger Instance
        {
            get
            {
                if (_cacheClientLicenseManager == null)
                {
                    lock (_creationMutex)
                    {
                        if (_cacheClientLicenseManager == null)
                            _cacheClientLicenseManager = new ClientLedger();
                    }
                }
                return _cacheClientLicenseManager;

            }
        }
    }
}
