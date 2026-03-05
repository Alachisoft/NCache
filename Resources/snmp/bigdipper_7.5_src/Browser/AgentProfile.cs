using System;
using System.Net;

namespace Lextm.SharpSnmpLib.Browser
{
    internal abstract class AgentProfile
    {
        internal AgentProfile(Guid id, VersionCode version, IPEndPoint agent, string name, string userName, int timeout)
        {
            Timeout = timeout;
            Id = id;
            UserName = userName;
            VersionCode = version;
            Agent = agent;
            Name = name;
        }

        internal int Timeout { get; private set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public IObjectRegistry Objects { get; set; }

        internal Guid Id { get; private set; }

        internal string Name { get; private set; }

        internal IPEndPoint Agent { get; private set; }

        internal VersionCode VersionCode { get; private set; }

        public string UserName { get; private set; }

        internal abstract void Get(Variable variable);

        internal abstract string GetValue(Variable variable);

        internal abstract void GetNext(Variable variable);

        internal abstract void Set(Variable variable);

        internal static bool IsValid(string address, out IPAddress ip)
        {
            return IPAddress.TryParse(address, out ip);
        }
        
        internal abstract void GetTable(IDefinition def);

        public abstract void Walk(IDefinition definition);
    }
}
