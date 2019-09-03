// $Id: MergeView.java,v 1.1.1.1 2003/09/09 01:24:08 belaban Exp $
using System;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups
{
	/// <summary> A view that is sent as result of a merge.</summary>
	[Serializable]
	internal class MergeView: View, ICloneable,ICompactSerializable
	{
		virtual public System.Collections.ArrayList Subgroups
		{
			get
			{
				return subgroups;
			}
			
		}
		protected internal System.Collections.ArrayList subgroups = null; // subgroups that merged into this single view (a list of Views)
		
		
		/// <summary> Used by externalization</summary>
		public MergeView()
		{
		}
		
		
		/// <summary> Creates a new view</summary>
		/// <param name="vid">      The view id of this view (can not be null)
		/// </param>
		/// <param name="members">  Contains a list of all the members in the view, can be empty but not null.
		/// </param>
		/// <param name="subgroups">A list of Views representing the former subgroups
		/// </param>
		public MergeView(ViewId vid, System.Collections.ArrayList members, System.Collections.ArrayList subgroups):base(vid, members)
		{
			this.subgroups = subgroups;
		}
		
		
		/// <summary> Creates a new view</summary>
		/// <param name="creator">  The creator of this view (can not be null)
		/// </param>
		/// <param name="id">       The lamport timestamp of this view
		/// </param>
		/// <param name="members">  Contains a list of all the members in the view, can be empty but not null.
		/// </param>
		/// <param name="subgroups">A list of Views representing the former subgroups
		/// </param>
		public MergeView(Address creator, long id, System.Collections.ArrayList members, System.Collections.ArrayList subgroups):base(creator, id, members)
		{
			this.subgroups = subgroups;
		}
		
		
		/// <summary> creates a copy of this view</summary>
		/// <returns> a copy of this view
		/// </returns>
		public override object Clone()
		{
			ViewId vid2 = Vid != null ? (ViewId)Vid.Clone() : null;
			System.Collections.ArrayList members2 = Members != null?(System.Collections.ArrayList) Members.Clone():null;
			System.Collections.ArrayList subgroups2 = subgroups != null?(System.Collections.ArrayList) subgroups.Clone():null;
			return new MergeView(vid2, members2, subgroups2);
		}
		
		
		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("MergeView::" + base.ToString());
			sb.Append(", subgroups=" + Global.CollectionToString(subgroups));
			return sb.ToString();
		}

		#region ICompactSerializable Members

		public override void Deserialize(CompactReader reader)
		{
			base.Deserialize(reader);
			subgroups = (System.Collections.ArrayList)reader.ReadObject();
		}

		public override void Serialize(CompactWriter writer)
		{
			base.Serialize(writer);
			writer.WriteObject(subgroups);
		}

		#endregion
	}
}