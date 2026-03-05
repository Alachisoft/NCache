using System;
using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;

namespace Lextm.SharpSnmpLib.Browser
{
    internal class SecureAgentProfile : AgentProfile
    {
        private readonly IAuthenticationProvider _auth;
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger("Lextm.SharpSnmpLib.Browser");
        private readonly IPrivacyProvider _privacy;
        private readonly UserRegistry _registry;

        public SecureAgentProfile(Guid id, VersionCode version, IPEndPoint agent, string agentName, string authenticationPassphrase, string privacyPassphrase, int authenticationMethod, int privacyMethod, string userName, int timeout)
            : base(id, version, agent, agentName, userName, timeout)
        {
            AuthenticationPassphrase = authenticationPassphrase;
            PrivacyPassphrase = privacyPassphrase;
            AuthenticationMethod = authenticationMethod;
            PrivacyMethod = privacyMethod;

            switch (AuthenticationMethod)
            {
                case 0:
                    _auth = DefaultAuthenticationProvider.Instance;
                    break;
                case 1:
                    _auth = new MD5AuthenticationProvider(new OctetString(AuthenticationPassphrase));
                    break;
                case 2:
                    _auth = new SHA1AuthenticationProvider(new OctetString(AuthenticationPassphrase));
                    break;
            }

            switch (PrivacyMethod)
            {
                case 0:
                    _privacy = new DefaultPrivacyProvider(_auth);
                    break;
                case 1:
                    _privacy = new DESPrivacyProvider(new OctetString(PrivacyPassphrase), _auth);
                    break;
                case 2:
                    _privacy = new AESPrivacyProvider(new OctetString(PrivacyPassphrase), _auth);
                    break;
            }
            
            _registry = new UserRegistry().Add(new OctetString(userName), _privacy);
        }

        internal string AuthenticationPassphrase { get; private set; }
        
        internal string PrivacyPassphrase { get; private set; }
        
        internal int AuthenticationMethod { get; private set; }
        
        internal int PrivacyMethod { get; private set; }

        public IPrivacyProvider Privacy
        {
            get { return _privacy; }
        }

        internal override void Get(Variable variable)
        {
            if (string.IsNullOrEmpty(UserName))
            {
                Logger.Info("User name need to be specified for v3.");
                return;
            }

            Discovery discovery = Messenger.NextDiscovery;
            ReportMessage report = discovery.GetResponse(Timeout, Agent);
            GetRequestMessage request = new GetRequestMessage(VersionCode.V3, Messenger.NextMessageId, Messenger.NextRequestId, new OctetString(UserName), new List<Variable> { variable }, _privacy, Messenger.MaxMessageSize, report);
            ISnmpMessage response = request.GetResponse(Timeout, Agent, _registry);
            if (response.Pdu().ErrorStatus.ToInt32() != 0)
            {
                throw ErrorException.Create(
                    "error in response",
                    Agent.Address,
                    response);
            }

            Logger.Info(response.Pdu().Variables[0].ToString(Objects));
        }

        internal override string GetValue(Variable variable)
        {
            Discovery discovery = Messenger.NextDiscovery;
            ReportMessage report = discovery.GetResponse(Timeout, Agent);
            GetRequestMessage request = new GetRequestMessage(VersionCode.V3, Messenger.NextMessageId, Messenger.NextRequestId, new OctetString(UserName), new List<Variable> { variable }, _privacy, Messenger.MaxMessageSize, report);
            ISnmpMessage response = request.GetResponse(Timeout, Agent, _registry);
            if (response.Pdu().ErrorStatus.ToInt32() != 0)
            {
                throw ErrorException.Create(
                    "error in response",
                    Agent.Address,
                    response);
            }

            return response.Pdu().Variables[0].Data.ToString();
        }

        internal override void GetNext(Variable variable)
        {
            if (string.IsNullOrEmpty(UserName))
            {
                Logger.Info("User name need to be specified for v3.");
                return;
            }

            Discovery discovery = Messenger.NextDiscovery;
            ReportMessage report = discovery.GetResponse(Timeout, Agent);
            GetNextRequestMessage request = new GetNextRequestMessage(VersionCode.V3, 100, 0, new OctetString(UserName), new List<Variable> { variable }, _privacy, Messenger.MaxMessageSize, report);
            ISnmpMessage response = request.GetResponse(Timeout, Agent, _registry);
            if (response.Pdu().ErrorStatus.ToInt32() != 0) 
            {
                throw ErrorException.Create(
                    "error in response",
                    Agent.Address,
                    response);
            }

            Logger.Info(response.Pdu().Variables[0].ToString(Objects));
        }

        internal override void Set(Variable variable)
        {
            if (string.IsNullOrEmpty(UserName))
            {
                Logger.Info("User name need to be specified for v3.");
                return;
            }

            Discovery discovery = Messenger.NextDiscovery;
            ReportMessage report = discovery.GetResponse(Timeout, Agent);
            SetRequestMessage request = new SetRequestMessage(VersionCode.V3, Messenger.NextMessageId, Messenger.NextRequestId, new OctetString(UserName), new List<Variable> { variable }, _privacy, Messenger.MaxMessageSize, report);
            ISnmpMessage response = request.GetResponse(Timeout, Agent, _registry);
            if (response.Pdu().ErrorStatus.ToInt32() != 0)
            {
                throw ErrorException.Create(
                    "error in response",
                    Agent.Address,
                    response);
            }

            Logger.Info(response.Pdu().Variables[0].ToString(Objects));
        }

        internal override void GetTable(IDefinition def)
        {
            Discovery discovery = Messenger.NextDiscovery;
            ReportMessage report = discovery.GetResponse(Timeout, Agent);
            IList<Variable> list = new List<Variable>();
            int rows = Messenger.BulkWalk(
                VersionCode.V3,
                Agent, 
                new OctetString(UserName),
                new ObjectIdentifier(def.GetNumericalForm()), 
                list, 
                Timeout, 
                10,
                WalkMode.WithinSubtree, 
                _privacy, 
                report);

            // How many rows are there?
            if (rows > 0)
            {
                FormTable newTable = new FormTable(def);
                newTable.SetRows(rows);
                newTable.PopulateGrid(list);
                newTable.Show();
            }
            else
            {
                foreach (Variable t in list)
                {
                    Logger.Info(t.ToString());
                }
            }
        }

        public override void Walk(IDefinition definition)
        {
            if (string.IsNullOrEmpty(UserName))
            {
                Logger.Info("User name need to be specified for v3.");
                return;
            }

            Discovery discovery = Messenger.NextDiscovery;
            ReportMessage report = discovery.GetResponse(Timeout, Agent);
            IList<Variable> list = new List<Variable>();
            Messenger.BulkWalk(
                VersionCode.V3,
                Agent, 
                new OctetString(UserName),
                new ObjectIdentifier(definition.GetNumericalForm()),
                list,
                Timeout, 
                10,
                WalkMode.WithinSubtree, 
                _privacy, 
                report);

            foreach (Variable v in list)
            {
                Logger.Info(v.ToString(Objects));
            }
        }
    }
}
