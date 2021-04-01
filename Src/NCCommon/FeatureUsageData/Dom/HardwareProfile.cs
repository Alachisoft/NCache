using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.FeatureUsageData.Dom
{
    [Serializable]
    public class HardwareProfile : ICloneable, ICompactSerializable
    {
        private int _cores;
        private string _memory;
        private string _operatingSystem;
        private string _activationStatus;
        private string _environemntName;
        private string _machineId;
        private string _machineName;
        private string _otherServers;

        public HardwareProfile()
        {
        }

        [ConfigurationAttribute("cores")]
        public int Cores
        {
            set { _cores = value; }
            get { return _cores; }
        }

        [ConfigurationAttribute("memory")]
        public string Memory
        {
            set { _memory = value; }
            get { return _memory; }
        }

        [ConfigurationAttribute("os")]
        public string OperatingSystem
        {
            set { _operatingSystem = value; }
            get { return _operatingSystem; }
        }

        [ConfigurationAttribute("activation-status")]
        public string ActivationStatus
        {
            set { _activationStatus = value; }
            get { return _activationStatus; }
        }

        [ConfigurationAttribute("environment-name")]
        public string EnvironmentName
        {
            get { return _environemntName; }
            set { _environemntName = value; }
        }

        [ConfigurationAttribute("machine-id")]
        public string MachineID
        {
            set { _machineId = value; }
            get { return _machineId; }
        }

        [ConfigurationAttribute("machine-name")]
        public string MachineName
        {
            set { _machineName = value; }
            get { return _machineName; }
        }


        [ConfigurationAttribute("other-servers")]
        public string OtherServers
        {
            get { return _otherServers; }
            set { _otherServers = value; }
        }
        public object Clone()
        {
            HardwareProfile hardwareProfile = new HardwareProfile();
            hardwareProfile.Cores = Cores;
            hardwareProfile.Memory = Memory;
            hardwareProfile.EnvironmentName = EnvironmentName;
            hardwareProfile.MachineID = MachineID;
            hardwareProfile.MachineName = MachineName;
            hardwareProfile.OtherServers = OtherServers;
            hardwareProfile.OperatingSystem = OperatingSystem;
            hardwareProfile.ActivationStatus = ActivationStatus;
            return hardwareProfile;
        }

        public void Deserialize(CompactReader reader)
        {
            _cores = reader.ReadInt32();
            _memory = reader.ReadObject() as string;
            _environemntName = reader.ReadObject() as string;
            _machineId = reader.ReadObject() as string;
            _machineName = reader.ReadObject() as string;
            _otherServers = reader.ReadObject() as string;
            _operatingSystem = reader.ReadObject() as string;
            _activationStatus = reader.ReadObject() as string;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_cores);
            writer.WriteObject(_memory);
            writer.WriteObject(_environemntName);
            writer.WriteObject(_machineId);
            writer.WriteObject(_machineName);
            writer.WriteObject(_otherServers);
            writer.WriteObject(_operatingSystem);
            writer.WriteObject(_activationStatus);
        }
    }
}
