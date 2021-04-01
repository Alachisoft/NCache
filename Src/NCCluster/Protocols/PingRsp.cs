using System;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups.Protocols
{
	/// <summary>
	/// Response object to request from PING
	/// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	[Serializable]
	internal class PingRsp : ICompactSerializable
	{
		/// <summary>Local Address</summary>
		private Address own_addr;
		/// <summary>Coordinator Address</summary>
		private Address coord_addr;
		/// <summary>Coordinator Address</summary>
		private bool	is_server;

        private bool    started = true;
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="own_addr">Local Address</param>
		/// <param name="coord_addr">Coordinator Address</param>
		/// <param name="is_server">if the node is a participant of group</param>
		public PingRsp(Address own_addr, Address coord_addr, bool is_server,bool started) 
		{
			this.own_addr=own_addr;
			this.coord_addr=coord_addr;
			this.is_server = is_server;
            this.started = started;

		}

		/// <summary> Gets the Local Address </summary>
		public Address OwnAddress { get{return own_addr;} }
		/// <summary> Gets the Coordinator Address </summary>
		public Address CoordAddress { get{return coord_addr;} }
		/// <summary> Gets the Coordinator Address </summary>
		public bool HasJoined { get{return is_server;} }

		/// <summary>
		/// Checks if the response is from the coordinator
		/// </summary>
		/// <returns>True if the response is from the coordinator</returns>
		public bool IsCoord 
		{
			get
			{
				if(own_addr != null && coord_addr != null)
					return own_addr.Equals(coord_addr);
				return false;
			}
		}

        public bool IsStarted
        {
            get { return started; }
        }
		public  override bool Equals(object obj)
		{
			PingRsp rsp = obj as PingRsp;
			if (rsp == null)
				return false;

			bool oe = false;
			if(own_addr != null)
			{
				oe = own_addr.Equals(rsp.own_addr);
			}
			else
				oe = (own_addr == rsp.own_addr);

			bool ce = false;
			if(own_addr != null)
			{
				ce = coord_addr.Equals(rsp.coord_addr);
			}
			else
				ce = (coord_addr == rsp.coord_addr);

			return  ce && oe;
		}

		/// <summary>
		/// Returns a string representation of the current object
		/// </summary>
		/// <returns>A string representation of the current object</returns>
		public override String ToString() 
		{
			return "[own_addr=" + own_addr + ", coord_addr=" + coord_addr + "]";
		}

		/// <summary>
		/// Retruns base hash code
		/// </summary>
		/// <returns>Base hash code</returns>
		public override int GetHashCode()
		{
			return base.GetHashCode ();
		}

		#region ICompactSerializable Members

		void ICompactSerializable.Deserialize(CompactReader reader)
		{
			own_addr = (Address)reader.ReadObject();
            coord_addr = (Address)reader.ReadObject();
			is_server = reader.ReadBoolean();
            started = reader.ReadBoolean();
		}

		void ICompactSerializable.Serialize(CompactWriter writer)
		{
			writer.WriteObject(own_addr);
            writer.WriteObject(coord_addr);
			writer.Write(is_server);
            writer.Write(started);
		}

		#endregion
	}
}
