using System;
using System.Net;

namespace Lextm.SharpSnmpLib.Browser
{
    internal static class AgentProfileFactory
    {
        internal static AgentProfile Create(Guid id, VersionCode version, IPEndPoint agent, string getCommunity, string setCommunity, string agentName, string authenticationPassphrase, string privacyPassphrase, int authenticationMethod, int privacyMethod, string userName, int timeout)
        {
            if (version == VersionCode.V3)
            {
                return new SecureAgentProfile(
                    id, 
                    version,
                    agent, 
                    agentName,
                    authenticationPassphrase, 
                    privacyPassphrase,
                    authenticationMethod, 
                    privacyMethod,
                    userName, 
                    timeout);
            }

            return new NormalAgentProfile(
                id, 
                version,
                agent, 
                new OctetString(getCommunity),
                new OctetString(setCommunity), 
                agentName,
                userName, 
                timeout);
        }
    }
}
