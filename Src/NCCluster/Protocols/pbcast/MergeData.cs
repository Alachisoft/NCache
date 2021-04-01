// $Id: MergeData.java,v 1.3 2004/09/06 13:55:40 belaban Exp $
using System;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups.Protocols.pbcast
{
	/// <summary> Encapsulates data sent with a MERGE_RSP (handleMergeResponse()) and INSTALL_MERGE_VIEW
	/// (handleMergeView()).
	/// 
	/// </summary>
	/// <author>  Bela Ban Oct 22 2001
	/// </author>
	[Serializable]
	internal class MergeData : ICompactSerializable
	{
		public Address Sender
		{
			get
			{
				return sender;
			}
			
		}
		public View View
		{
			get
			{
				return view;
			}
			
			set
			{
				view = value;
			}
			
		}
		public Digest Digest
		{
			get
			{
				return digest;
			}
			
			set
			{
				digest = value;
			}
			
		}
		internal Address sender = null;
		internal bool merge_rejected = false;
		internal View view = null;
		internal Digest digest = null;
		
		/// <summary> Empty constructor needed for externalization</summary>
		public MergeData()
		{
			;
		}
		
		public MergeData(Address sender, View view, Digest digest)
		{
			this.sender = sender;
			this.view = view;
			this.digest = digest;
		}
		
		
		public  override bool Equals(object other)
		{
			return sender != null && other != null && other is MergeData && ((MergeData) other).sender != null && ((MergeData) other).sender.Equals(sender);
		}
		
		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("sender=" + sender);
			if (merge_rejected)
				sb.Append(" (merge_rejected)");
			else
			{
				sb.Append(", view=" + view + ", digest=" + digest);
			}
			return sb.ToString();
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		#region ICompactSerializable Members

		void ICompactSerializable.Deserialize(CompactReader reader)
		{
			sender = (Address)reader.ReadObject();
			merge_rejected = reader.ReadBoolean();
			if (!merge_rejected)
			{
                view = (View)reader.ReadObject();
                digest = (Digest)reader.ReadObject();
			}
		}

		void ICompactSerializable.Serialize(CompactWriter writer)
		{
			writer.WriteObject(sender);
			writer.Write(merge_rejected);
			if (!merge_rejected)
			{
                writer.WriteObject(view);
                writer.WriteObject(digest);
			}
		}

		#endregion
	}
}