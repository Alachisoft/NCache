// $Id: Digest.java,v 1.6 2004/07/05 05:49:41 belaban Exp $
using System;

using Alachisoft.NGroups;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;


using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups.Protocols.pbcast
{
	/// <summary> A message digest, which is used e.g. by the PBCAST layer for gossiping (also used by NAKACK for
	/// keeping track of current seqnos for all members). It contains pairs of senders and a range of seqnos
	/// (low and high), where each sender is associated with its highest and lowest seqnos seen so far.  That
	/// is, the lowest seqno which was not yet garbage-collected and the highest that was seen so far and is
	/// deliverable (or was already delivered) to the application.  A range of [0 - 0] means no messages have
	/// been received yet. <p> April 3 2001 (bela): Added high_seqnos_seen member. It is used to disseminate
	/// information about the last (highest) message M received from a sender P. Since we might be using a
	/// negative acknowledgment message numbering scheme, we would never know if the last message was
	/// lost. Therefore we periodically gossip and include the last message seqno. Members who haven't seen
	/// it (e.g. because msg was dropped) will request a retransmission. See DESIGN for details.
	/// </summary>
	/// <author>  Bela Ban
	/// </author>
	[Serializable]
	internal class Digest : ICompactSerializable
	{
		internal Address[] senders = null;
		internal long[] low_seqnos = null; // lowest seqnos seen
		internal long[] high_seqnos = null; // highest seqnos seen so far *that are deliverable*, initially 0
		internal long[] high_seqnos_seen = null; // highest seqnos seen so far (not necessarily deliverable), initially -1
		internal int index = 0; // current index of where next member is added
		
		public Digest()
		{
		} // used for externalization
		
		public Digest(int size)
		{
			reset(size);
		}
		
		
		public void  add(Address sender, long low_seqno, long high_seqno)
		{
			if (index >= senders.Length)
			{
				return ;
			}
			if (sender == null)
			{
				return ;
			}
			senders[index] = sender;
			low_seqnos[index] = low_seqno;
			high_seqnos[index] = high_seqno;
			high_seqnos_seen[index] = - 1;
			index++;
		}
		
		
		public void  add(Address sender, long low_seqno, long high_seqno, long high_seqno_seen)
		{
			if (index >= senders.Length)
			{
				return ;
			}
			if (sender == null)
			{
				return ;
			}
			senders[index] = sender;
			low_seqnos[index] = low_seqno;
			high_seqnos[index] = high_seqno;
			high_seqnos_seen[index] = high_seqno_seen;
			index++;
		}
		
		
		public void  add(Digest d)
		{
			Address sender;
			long low_seqno, high_seqno, high_seqno_seen;
			
			if (d != null)
			{
				for (int i = 0; i < d.size(); i++)
				{
					sender = d.senderAt(i);
					low_seqno = d.lowSeqnoAt(i);
					high_seqno = d.highSeqnoAt(i);
					high_seqno_seen = d.highSeqnoSeenAt(i);
					add(sender, low_seqno, high_seqno, high_seqno_seen);
				}
			}
		}
		
		
		/// <summary> Adds a digest to this digest. This digest must have enough space to add the other digest; otherwise an error
		/// message will be written. For each sender in the other digest, the merge() method will be called.
		/// </summary>
		public void  merge(Digest d)
		{
			Address sender;
			long low_seqno, high_seqno, high_seqno_seen;
			
			if (d == null)
			{
				return ;
			}
			for (int i = 0; i < d.size(); i++)
			{
				sender = d.senderAt(i);
				low_seqno = d.lowSeqnoAt(i);
				high_seqno = d.highSeqnoAt(i);
				high_seqno_seen = d.highSeqnoSeenAt(i);
				merge(sender, low_seqno, high_seqno, high_seqno_seen);
			}
		}
		
		
		/// <summary> Similar to add(), but if the sender already exists, its seqnos will be modified (no new entry) as follows:
		/// <ol>
		/// <li>this.low_seqno=min(this.low_seqno, low_seqno)
		/// <li>this.high_seqno=max(this.high_seqno, high_seqno)
		/// <li>this.high_seqno_seen=max(this.high_seqno_seen, high_seqno_seen)
		/// </ol>
		/// If the sender doesn not exist, a new entry will be added (provided there is enough space)
		/// </summary>
		public void  merge(Address sender, long low_seqno, long high_seqno, long high_seqno_seen)
		{
			int index;
			long my_low_seqno, my_high_seqno, my_high_seqno_seen;
			if (sender == null)
			{
				return ;
			}
			index = getIndex(sender);
			if (index == - 1)
			{
				add(sender, low_seqno, high_seqno, high_seqno_seen);
				return ;
			}
			
			my_low_seqno = lowSeqnoAt(index);
			my_high_seqno = highSeqnoAt(index);
			my_high_seqno_seen = highSeqnoSeenAt(index);
			if (low_seqno < my_low_seqno)
				setLowSeqnoAt(index, low_seqno);
			if (high_seqno > my_high_seqno)
				setHighSeqnoAt(index, high_seqno);
			if (high_seqno_seen > my_high_seqno_seen)
				setHighSeqnoSeenAt(index, high_seqno_seen);
		}
		
		
		public int getIndex(Address sender)
		{
			int ret = - 1;
			
			if (sender == null)
				return ret;
			for (int i = 0; i < senders.Length; i++)
				if (sender.Equals(senders[i]))
					return i;
			return ret;
		}
		
		
		public bool contains(Address sender)
		{
			return getIndex(sender) != - 1;
		}
		
		
		/// <summary> Compares two digests and returns true if the senders are the same, otherwise false</summary>
		/// <param name="">other
		/// </param>
		/// <returns>
		/// </returns>
		public bool sameSenders(Digest other)
		{
			Address a1, a2;
			if (other == null)
				return false;
			if (this.senders == null || other.senders == null)
				return false;
			if (this.senders.Length != other.senders.Length)
				return false;
			for (int i = 0; i < this.senders.Length; i++)
			{
				a1 = this.senders[i];
				a2 = other.senders[i];
				if (a1 == null && a2 == null)
					continue;
				if (a1 != null && a2 != null && a1.Equals(a2))
					continue;
				else
					return false;
			}
			return true;
		}
		
		/// <summary>Increment the sender's high_seqno by 1 </summary>
		public void  incrementHighSeqno(Address sender)
		{
			if (sender == null)
				return ;
			for (int i = 0; i < senders.Length; i++)
			{
				if (senders[i] != null && senders[i].Equals(sender))
				{
					high_seqnos[i] = high_seqnos[i] + 1;
					break;
				}
			}
		}
		
		
		public int size()
		{
			return senders.Length;
		}
		
		
		public Address senderAt(int index)
		{
			if (index < size())
				return senders[index];
			else
			{
				return null;
			}
		}
		
		
		/// <summary> Resets the seqnos for the sender at 'index' to 0. This happens when a member has left the group,
		/// but it is still in the digest. Resetting its seqnos ensures that no-one will request a message
		/// retransmission from the dead member.
		/// </summary>
		public void  resetAt(int index)
		{
			if (index < size())
			{
				low_seqnos[index] = 0;
				high_seqnos[index] = 0;
				high_seqnos_seen[index] = - 1;
			}
		}
		
		
		public void  reset(int size)
		{
			senders = new Address[size];
			low_seqnos = new long[size];
			high_seqnos = new long[size];
			high_seqnos_seen = new long[size];
			for (int i = 0; i < size; i++)
				high_seqnos_seen[i] = - 1;
			index = 0;
		}
		
		
		public long lowSeqnoAt(int index)
		{
			if (index < size())
				return low_seqnos[index];
			else
			{
				return 0;
			}
		}
		
		
		public long highSeqnoAt(int index)
		{
			if (index < size())
				return high_seqnos[index];
			else
			{
				return 0;
			}
		}
		
		public long highSeqnoSeenAt(int index)
		{
			if (index < size())
				return high_seqnos_seen[index];
			else
			{
				return 0;
			}
		}
		
		
		public long highSeqnoAt(Address sender)
		{
			long ret = - 1;
			int index;
			
			if (sender == null)
				return ret;
			index = getIndex(sender);
			if (index == - 1)
				return ret;
			else
				return high_seqnos[index];
		}
		
		
		public long highSeqnoSeenAt(Address sender)
		{
			long ret = - 1;
			int index;
			
			if (sender == null)
				return ret;
			index = getIndex(sender);
			if (index == - 1)
				return ret;
			else
				return high_seqnos_seen[index];
		}
		
		public void  setLowSeqnoAt(int index, long low_seqno)
		{
			if (index < size())
			{
				low_seqnos[index] = low_seqno;
			}
		}
		
		
		public void  setHighSeqnoAt(int index, long high_seqno)
		{
			if (index < size())
			{
				high_seqnos[index] = high_seqno;
			}
		}
		
		public void  setHighSeqnoSeenAt(int index, long high_seqno_seen)
		{
			if (index < size())
			{
				high_seqnos_seen[index] = high_seqno_seen;
			}
		}
		
		
		public void  setHighSeqnoAt(Address sender, long high_seqno)
		{
			int index = getIndex(sender);
			if (index < 0)
				return ;
			else
				setHighSeqnoAt(index, high_seqno);
		}
		
		public void  setHighSeqnoSeenAt(Address sender, long high_seqno_seen)
		{
			int index = getIndex(sender);
			if (index < 0)
				return ;
			else
				setHighSeqnoSeenAt(index, high_seqno_seen);
		}
		
		
		public Digest copy()
		{
			Digest ret = new Digest(senders.Length);
			
			if (senders != null)
				Array.Copy(senders, 0, ret.senders, 0, senders.Length);
			
			ret.low_seqnos = new long[low_seqnos.Length];
			low_seqnos.CopyTo(ret.low_seqnos, 0);
			ret.high_seqnos = new long[high_seqnos.Length];
			high_seqnos.CopyTo(ret.high_seqnos, 0);
			ret.high_seqnos_seen = new long[high_seqnos_seen.Length];
			high_seqnos_seen.CopyTo(ret.high_seqnos_seen, 0);
			return ret;
		}
		
		
		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			bool first = true;
			if (senders == null)
				return "[]";
			for (int i = 0; i < senders.Length; i++)
			{
				if (!first)
				{
					sb.Append(", ");
				}
				else
				{
					sb.Append('[');
					first = false;
				}
				sb.Append(senders[i]).Append(": ").Append('[').Append(low_seqnos[i]).Append(" : ");
				sb.Append(high_seqnos[i]);
				if (high_seqnos_seen[i] >= 0)
					sb.Append(" (").Append(high_seqnos_seen[i]).Append(")]");
			}
			sb.Append(']');
			return sb.ToString();
		}
		
		
		public string printHighSeqnos()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			bool first = true;
			for (int i = 0; i < senders.Length; i++)
			{
				if (!first)
				{
					sb.Append(", ");
				}
				else
				{
					sb.Append('[');
					first = false;
				}
				sb.Append(senders[i]);
				sb.Append('#');
				sb.Append(high_seqnos[i]);
			}
			sb.Append(']');
			return sb.ToString();
		}
		
		
		public string printHighSeqnosSeen()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			bool first = true;
			for (int i = 0; i < senders.Length; i++)
			{
				if (!first)
				{
					sb.Append(", ");
				}
				else
				{
					sb.Append('[');
					first = false;
				}
				sb.Append(senders[i]);
				sb.Append('#');
				sb.Append(high_seqnos_seen[i]);
			}
			sb.Append(']');
			return sb.ToString();
		}
		
		#region ICompactSerializable Members

		void ICompactSerializable.Deserialize(CompactReader reader)
		{
			senders = (Address[])reader.ReadObject();
			low_seqnos = (long[])reader.ReadObject();
            high_seqnos = (long[])reader.ReadObject();
            high_seqnos_seen = (long[])reader.ReadObject();
			index = reader.ReadInt32();
		}

		void ICompactSerializable.Serialize(CompactWriter writer)
		{
			writer.WriteObject(senders);
            writer.WriteObject(low_seqnos);
            writer.WriteObject(high_seqnos);
            writer.WriteObject(high_seqnos_seen);
			writer.Write(index);
		}

		#endregion
	}
}