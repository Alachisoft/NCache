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
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Common.Monitoring
{
	
	[Serializable]
	public class ClientProcessStats : ClientNode, IComparable,ICompactSerializable 
	{
		private string _processID;
		private float _bytesSent;
		private float _bytesReceived;
		private string _serverIPAddress;
		public string ServerIPAddress
		{
			get { return _serverIPAddress; }
			set { _serverIPAddress = value; }
		}

		public string ProcessID
		{
			get { return _processID; }
		}

		public float BytesSent
		{
			get { return _bytesSent; }
			set { _bytesSent = value; }
		}

		public float BytesReceived
		{
			get { return _bytesReceived; }
			set { _bytesReceived = value; }
		}

		public ClientProcessStats(string clientID, Address address, float byteSent, float byteReceived, string serverIPAddress)
		{
			Address = address;
			ClientID = clientID;
            int lastIndex = clientID.LastIndexOf(":");
			if (lastIndex != -1 && lastIndex != clientID.Length-1)
			{
				_processID = clientID.Substring(lastIndex + 1, clientID.Length - lastIndex - 1);
			}
			_bytesReceived = byteReceived;
			_bytesSent = byteSent;
			_serverIPAddress = serverIPAddress;
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			ClientProcessStats clientProcessStats = obj as ClientProcessStats;
			if (clientProcessStats == null)
				return -1;

			int result = 0;

			result = _processID.CompareTo(clientProcessStats._processID);

			if (result != 0)
			{
				result = Address.IpAddress.ToString().CompareTo(clientProcessStats.Address.IpAddress.ToString());
			}

			if (result != 0)
			{
				result = Address.Port.ToString().CompareTo(Address.Port.ToString());
			}

			if (result != 0)
			{
				result = _serverIPAddress.CompareTo(clientProcessStats.ServerIPAddress);
			}

			if (result != 0)
			{
				result = _bytesSent.CompareTo(clientProcessStats.BytesSent);
			}

			if (result != 0)
			{
				result = _bytesReceived.CompareTo(clientProcessStats.BytesReceived);
			}

			return result;
		}

        #endregion

        #region ICompactSerializable Members

        public new void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _processID = reader.ReadObject() as string;
            _bytesSent = reader.ReadSingle();
            _bytesReceived = reader.ReadSingle();
            _serverIPAddress = reader.ReadObject() as string;
            base.Deserialize(reader);
        }

        public new void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_processID);
            writer.Write(_bytesSent);
            writer.Write(_bytesReceived);
            writer.WriteObject(_serverIPAddress);
            base.Serialize(writer);
        }

        #endregion
    }
}
