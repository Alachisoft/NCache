using System;
using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Browser
{
    internal interface IProfileRegistry
    {
        AgentProfile DefaultProfile { get; set; }
        
        IEnumerable<AgentProfile> Profiles { get; }
        
        void AddProfile(AgentProfile profile);
        
        void DeleteProfile(AgentProfile profile);
        
        void LoadProfiles();
        
        void SaveProfiles();
        
        event EventHandler<EventArgs> OnChanged;
    }
}