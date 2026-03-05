using System;
using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib.Messaging;

namespace Lextm.SharpSnmpLib.Browser
{
    internal class NormalAgentProfile : AgentProfile
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger("Lextm.SharpSnmpLib.Browser");

        public NormalAgentProfile(Guid id, VersionCode version, IPEndPoint agent, OctetString getCommunity, OctetString setCommunity, string agentName, string userName, int timeout)
            : base(id, version, agent, agentName, userName, timeout)
        {
            GetCommunity = getCommunity;
            SetCommunity = setCommunity;
        }

        internal OctetString GetCommunity { get; set; }
        
        internal OctetString SetCommunity { get; set; }

        internal override void Get(Variable variable)
        {
            Logger.Info(Messenger.Get(VersionCode, Agent, GetCommunity, new List<Variable> { variable }, Timeout)[0].ToString(Objects));
        }

        internal override string GetValue(Variable variable)
        {
            return Messenger.Get(VersionCode, Agent, GetCommunity, new List<Variable> { variable }, Timeout)[0].Data.ToString();
        }

        internal override void GetNext(Variable variable)
        {
            GetNextRequestMessage message = new GetNextRequestMessage(
                Messenger.NextRequestId,
                VersionCode, 
                GetCommunity,
                new List<Variable> { variable });
            ISnmpMessage response = message.GetResponse(Timeout, Agent);
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
            Logger.Info(Messenger.Set(VersionCode, Agent, SetCommunity, new List<Variable> { variable }, Timeout)[0].ToString(Objects));
        }

        internal override void GetTable(IDefinition def)
        {
            IList<Variable> list = new List<Variable>();
            int rows = Messenger.Walk(VersionCode, Agent, GetCommunity, new ObjectIdentifier(def.GetNumericalForm()), list, Timeout, WalkMode.WithinSubtree);
            
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
            IList<Variable> list = new List<Variable>();
            if (VersionCode == VersionCode.V1)
            {
                Messenger.Walk(
                    VersionCode, 
                    Agent, 
                    GetCommunity,
                    new ObjectIdentifier(definition.GetNumericalForm()),
                    list,
                    Timeout,
                    WalkMode.WithinSubtree);
            }
            else
            {
                Messenger.BulkWalk(
                    VersionCode,
                    Agent, 
                    GetCommunity,
                    new ObjectIdentifier(definition.GetNumericalForm()), 
                    list, 
                    Timeout,
                    10,
                    WalkMode.WithinSubtree, 
                    null, 
                    null);
            }

            foreach (Variable v in list)
            {
                Logger.Info(v.ToString(Objects));
            }
        }
    }
}