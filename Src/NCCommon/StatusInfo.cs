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

using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Common
{
    [Serializable]
    public class StatusInfo: ICompactSerializable
    {
        private const string NODE_EXPIRED_MESSAGE = "Your license for using NCache has expired on {0}. Please contact sales@alachisoft.com for further terms and conditions.";
        private const string NODE_EXPIRED_MESSAGE2 = "Your license for using NCache has expired. Please contact sales@alachisoft.com for further terms and conditions.";
                private string _info = "";
        /// <summary>Status of the Cache.</summary>
        public CacheStatus Status = CacheStatus.Unavailable;
        internal bool IsCoordinatorInternal = false;
        string configID;
        double configVersion;

        /// <summary>
        /// This property tells whether the node is active node in
        /// mirror topology.
        /// </summary>
        public bool IsCoordinator
        {
            get { return IsCoordinatorInternal; }
            set { IsCoordinatorInternal = value; }
        }

        /// <summary>
        /// Tells the unique GUID for the configuration.
        /// <para>This helps in identifying the inconsistency b/w project file and config.ncconf</para>
        /// </summary>
        public string ConfigID
        {
            get { return configID; }
            set { configID = value; }
        }

        /// <summary>
        /// Tells the unique sequence number of the last applied configuration.
        /// <para>This helps in identifying the inconsistency b/w project file and config.ncconf</para>
        /// </summary>
        public double ConfigVersion
        {
            get { return configVersion; }
            set { configVersion = value; }
        }

        /// <summary>
        /// Information about the current status of the cache.
        /// </summary>
        /// <param name="nodeName">The name of the status node. This name is used to format the message string.</param>
        /// <returns>Formated message string.</returns>
        public string Info(string nodeName)
        {
            switch (Status)
            {
                case CacheStatus.Expired:
                    if (nodeName == null || nodeName == string.Empty)
                        _info = NODE_EXPIRED_MESSAGE2;
                    else
                        _info = string.Format(NODE_EXPIRED_MESSAGE, nodeName);
                    break;
                case CacheStatus.Registered:
                    _info = "Stopped";
                    break;
                case CacheStatus.Running:
                case CacheStatus.Unavailable:
                    _info = Status.ToString();
                    break;
                default:
                    _info = "Stopped";
                    break;
            }
            return _info;
        }

        public StatusInfo()
            : this(CacheStatus.Unavailable)
        { }
        public StatusInfo(CacheStatus status)
            : this(status, "")
        { }

        public StatusInfo(CacheStatus status, string info)
        {
            Status = status;
            _info = info;
        }

        public bool IsRunning { get { return Status == CacheStatus.Running; } }
        public bool IsUnavailable { get { return Status == CacheStatus.Unavailable; } }
        public bool IsExpired { get { return Status == CacheStatus.Expired; } }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _info = reader.ReadObject() as string;
            Status = (CacheStatus)reader.ReadInt32();
            IsCoordinatorInternal = reader.ReadBoolean();
            configID = reader.ReadString();
            configVersion = reader.ReadDouble();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {           
            writer.WriteObject(_info);
            writer.Write((int)Status);
            writer.Write(IsCoordinatorInternal);
            writer.Write(configID);
            writer.Write(configVersion);
        }

        #endregion
    }
}
