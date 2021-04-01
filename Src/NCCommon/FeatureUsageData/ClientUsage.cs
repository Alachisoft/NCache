using Alachisoft.NCache.Common.FeatureUsageData.Dom;
using Alachisoft.NCache.Common.Monitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Alachisoft.NCache.Common.FeatureUsageData
{
    public class ClientUsage
    {
        private long MaximumCores { get; set; }

        private long MinimunCores { get; set; }

        private long MaximumMemory { get; set; }

        private long MinimunMemory { get; set; }

        private List<string> OperatingSystem = new List<string>();

        private List<string> Platform = new List<string>();

        private long MaximumConnectedClients { get; set; }

        public void UpdateClientUsageProfile(ClientProfile clientProfile)
        {

            if (MaximumCores == 0)
                MaximumCores = clientProfile.Cores;
            if (MinimunCores == 0)
                MinimunCores = clientProfile.Cores;


            if (clientProfile.Cores > MaximumCores)
            {
                MaximumCores = clientProfile.Cores;
            }
            else if (clientProfile.Cores < MinimunCores)
            {
                MinimunCores = clientProfile.Cores;
            }

            if (MaximumMemory == 0)
                MaximumMemory = clientProfile.Memory;
            if (MinimunMemory == 0)
                MinimunMemory = clientProfile.Memory;

            if (clientProfile.Memory > MaximumMemory)
            {
                MaximumMemory = clientProfile.Memory;
            }
            if (clientProfile.Memory < MinimunMemory)
            {
                MinimunMemory = clientProfile.Memory;
            }

            UpdateClientOperationSystemAndProfile(clientProfile.OperatingSystem, clientProfile.Platform);

        }


        public void UpdateClientUsageProfile(ClientProfileDom clientProfile)
        {

            if (MaximumConnectedClients < clientProfile.MaximumConnectedClients)
                MaximumConnectedClients = clientProfile.MaximumConnectedClients;

            if (MaximumCores == 0)
                MaximumCores = clientProfile.MaximumCores;
            if (MinimunCores == 0)
                MinimunCores = clientProfile.MinimumCores;


            if (clientProfile.MaximumCores > MaximumCores)
                MaximumCores = clientProfile.MaximumCores;

            if (clientProfile.MinimumCores < MinimunCores)
            {
                MinimunCores = clientProfile.MinimumCores;
            }

            if (MaximumMemory == 0)
                MaximumMemory = clientProfile.MaximumMemory;
            if (MinimunMemory == 0)
                MinimunMemory = clientProfile.MinimumMemory;

            if (clientProfile.MaximumMemory > MaximumMemory)
                MaximumMemory = clientProfile.MaximumMemory;

            if (clientProfile.MinimumMemory < MinimunMemory)
            {
                MinimunMemory = clientProfile.MinimumMemory;
            }

            UpdateClientOperationSystemAndProfile(clientProfile.OperatingSystem, clientProfile.Platform);

        }



        public void UpdateConfigUsageProfile(ClientProfileDom clientProfile)
        {

            if (MaximumConnectedClients < clientProfile.MaximumConnectedClients)
                MaximumConnectedClients = clientProfile.MaximumConnectedClients;

            if (MaximumCores == 0)
                MaximumCores = clientProfile.MaximumCores;
            if (MinimunCores == 0)
                MinimunCores = clientProfile.MinimumCores;


            if (clientProfile.MaximumCores > MaximumCores)
                MaximumCores = clientProfile.MaximumCores;

            if (clientProfile.MinimumCores < MinimunCores)
            {
                MinimunCores = clientProfile.MinimumCores;
            }

            if (MaximumMemory == 0)
                MaximumMemory = clientProfile.MaximumMemory;
            if (MinimunMemory == 0)
                MinimunMemory = clientProfile.MinimumMemory;

            if (clientProfile.MaximumMemory > MaximumMemory)
                MaximumMemory = clientProfile.MaximumMemory;

            if (clientProfile.MinimumMemory < MinimunMemory)
            {
                MinimunMemory = clientProfile.MinimumMemory;
            }

            UpdateOperationSystemAndProfile(clientProfile.OperatingSystem, clientProfile.Platform);

        }

        public ClientProfileDom GetClientProfile(long clientCount = -1)
        {
            return new ClientProfileDom()
            {
                MaximumConnectedClients = clientCount != -1 ? clientCount : MaximumConnectedClients,
                MaximumCores = MaximumCores == -1 ? 0 : MaximumCores,
                MaximumMemory = MaximumMemory == -1 ? 0 : MaximumMemory,
                MinimumCores = MinimunCores == -1 ? 0 : MinimunCores,
                MinimumMemory = MinimunMemory == -1 ? 0 : MinimunMemory,
                OperatingSystem = ClientProfileDom.ListToStringConvert(OperatingSystem),
                Platform = ClientProfileDom.ListToStringConvert(Platform)
            };
        }

        private void UpdateClientOperationSystemAndProfile(string clientOperatingSystem, string clientPlatForm)
        {

            List<string> OSList = ClientProfileDom.StringToList(clientOperatingSystem);
            List<string> platFormList = ClientProfileDom.StringToList(clientPlatForm);


            foreach (var item in OSList)
            {
                if (!OperatingSystem.Contains(item))
                {
                    OperatingSystem.Add(item);
                }
            }

            foreach (var item in platFormList)
            {
                if (!Platform.Contains(item))
                {
                    Platform.Add(item);
                }
            }

        }

        private void UpdateOperationSystemAndProfile(string clientOperatingSystem, string clientPlatForm)
        {

            List<string> OSList = ClientProfileDom.StringToList(clientOperatingSystem);
            List<string> platFormList = ClientProfileDom.StringToList(clientPlatForm);

            foreach (var item in OperatingSystem)
            {
                if (!OSList.Contains(item))
                {
                    OSList.Add(item);
                }
            }

            OperatingSystem = OSList;

            foreach (var item in Platform)
            {
                if (!platFormList.Contains(item))
                {
                    platFormList.Add(item);
                }
            }

            Platform = platFormList;

        }
    }
}
