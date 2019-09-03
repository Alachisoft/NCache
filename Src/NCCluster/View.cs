using System;
using System.Collections;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Mirroring;

namespace Alachisoft.NGroups
{
	/// <summary>
	/// Represents the current 'View' of the _members of the group
	/// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	[Serializable]
	public class View : ICloneable, ICompactSerializable
	{
		/// <remarks>
		/// The view id contains the creator address and a lamport time.
		/// the lamport time is the highest timestamp seen or sent from a view.
		/// if a view change comes in with a lower lamport time, the event is discarded.
		/// </remarks>
		/// <summary>
		/// A view is uniquely identified by its ViewID
		/// </summary>
		private ViewId vid;

		/// <remarks>
		/// This list is always ordered, with the coordinator being the first member.
		/// the second member will be the new coordinator if the current one disappears
		/// or leaves the group.
		/// </remarks>
		/// <summary>
		/// A list containing all the _members of the view
		/// </summary>
		private ArrayList  _members;

		/// <summary>
		/// contains the mbrs list agains subgroups
		/// </summary>
		private Hashtable _sequencerTbl;

		/// <summary>
		/// contains the subgroup against the mbr addresses.
		/// </summary>
		private Hashtable _mbrsSubgroupMap;

        private bool _forceInstall;

        private DistributionMaps _distributionMaps;
        private string _coordinatorGmsId;

        /// <summary>
        /// Map table or some serialized link list for dynamic mirroring.
        /// </summary>
        private CacheNode[] _mirrorMapping;
        
        private Hashtable nodeGmsIds = new Hashtable();
		/// <summary> creates an empty view, should not be used</summary>
		public View()
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="vid">The view id of this view (can not be null)</param>
		/// <param name="_members">Contains a list of all the _members in the view, can be empty but not null.</param>
		public View(ViewId vid, ArrayList _members)
		{
			this.vid = vid;
			this._members = _members;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="vid">The view id of this view (can not be null)</param>
		/// <param name="_members">Contains a list of all the _members in the view, can be empty but not null.</param>
		public View(ViewId vid, ArrayList _members, Hashtable sequencerTbl)
		{
			this.vid = vid;
			this._members = _members;
			this._sequencerTbl = sequencerTbl;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="creator">The creator of this view</param>
		/// <param name="id">The lamport timestamp of this view</param>
		/// <param name="_members">Contains a list of all the _members in the view, can be empty but not null.</param>
		public View(Address creator, long id, ArrayList _members) : this( new ViewId(creator, id), _members)
		{
		}

		/// <summary> returns the view ID of this view
		/// if this view was created with the empty constructur, null will be returned
		/// </summary>
		/// <returns> the view ID of this view
		/// </returns>
		public ViewId Vid
		{
			get
			{
				return vid;
			}
			
		}

        public bool ForceInstall { get { return _forceInstall; } set { _forceInstall = value; } }

        public string CoordinatorGmsId { get { return _coordinatorGmsId; } set { _coordinatorGmsId = value; } }

        public DistributionMaps DistributionMaps
        {
            get { return _distributionMaps; }
            set { _distributionMaps = value; }
        }

        public Address Coordinator
        {
            get { return _members != null && _members.Count > 0 ? _members[0] as Address : null; }
        }
		public Hashtable SequencerTbl
		{
			get { return this._sequencerTbl; }
			set { this._sequencerTbl = value; }
		}

		public Hashtable MbrsSubgroupMap
		{
			get { return this._mbrsSubgroupMap; }
			set { this._mbrsSubgroupMap = value; }
		}
        
		/// <summary> returns the creator of this view
		/// if this view was created with the empty constructur, null will be returned
		/// </summary>
		/// <returns> the creator of this view in form of an Address object
		/// </returns>
		public Address Creator
		{
			get
			{
				return vid != null?vid.CoordAddress:null;
			}
		}

        public void AddGmsId(Address node, string id)
        {
            if(node != null)
                nodeGmsIds[node] = id;
        }

        public void RemoveGmsId(Address node)
        {
            if(node != null)
                nodeGmsIds.Remove(node);
        }

        public void RemoveGmsId(ArrayList nodes)
        {
            foreach (Address node in nodes)
            {
                if(node != null)
                    nodeGmsIds.Remove(node);
            }
        }
        public Hashtable GmsIds
        {
            get { return nodeGmsIds; }
        }

        public string GetGmsId(Address node)
        {
            return nodeGmsIds[node] as string;
        }
		/// <summary> Returns a reference to the List of _members (ordered)
		/// Do NOT change this list, hence your will invalidate the view
		/// Make a copy if you have to modify it.
		/// </summary>
		/// <returns> a reference to the ordered list of _members in this view
		/// </returns>
		public System.Collections.ArrayList Members
		{
			get
			{
				return _members;
			}
		}

        /// <summary>
        /// Hashtable or some serialized object used for dynamic mirroring in 
        /// case of Partitioned Replica topology. This along with Distribution Map 
        /// is sent back to the joining node or to all the nodes in the cluster in case of leaving.
        /// </summary>
        public CacheNode[] MirrorMapping
        {
            get { return _mirrorMapping; }
            set { _mirrorMapping = value; }
        }


		/// <summary>
		/// Returns true, if this view contains a certain member
		/// </summary>
		/// <param name="mbr">The address of the member</param>
		/// <returns>True, if this view contains a certain member</returns>
		public bool containsMember( Address mbr )
		{
			if ( mbr == null || _members == null )
			{
				return false;
			}
			return _members.Contains(mbr);
		}

		/// <summary>
		/// Returns the number of _members in this view
		/// </summary>
		/// <returns>The number of _members in this view</returns>
		public int size()
		{
			if (_members == null)
				return 0;
			else
				return _members.Count;
		}

	
		/// <summary> creates a copy of this view</summary>
		/// <returns> a copy of this view
		/// </returns>
		public virtual object Clone()
		{
			ViewId vid2 = vid != null?(ViewId) vid.Clone():null;
			System.Collections.ArrayList members2 = _members != null?(System.Collections.ArrayList) _members.Clone():null;
			View v = new View(vid2, members2);
			if(SequencerTbl != null)
				v.SequencerTbl = SequencerTbl.Clone() as Hashtable;
			if (MbrsSubgroupMap != null)
				v.MbrsSubgroupMap = MbrsSubgroupMap.Clone() as Hashtable;

            v._coordinatorGmsId = _coordinatorGmsId;

            if (DistributionMaps != null)
                v.DistributionMaps = DistributionMaps.Clone() as DistributionMaps;

            if (MirrorMapping != null)
                v.MirrorMapping = MirrorMapping;

            if (nodeGmsIds != null) v.nodeGmsIds = nodeGmsIds.Clone() as Hashtable;

            return (v);
		}

		/// <summary>
		/// Copys the View
		/// </summary>
		/// <returns>A copy of the View</returns>
		public View Copy()
		{
			return (View)Copy();
		}
		
		/// <summary>
		/// Returns a string representation of the View
		/// </summary>
		/// <returns>A string representation of the View</returns>
		public override String ToString()
		{
			System.Text.StringBuilder ret = new System.Text.StringBuilder();
			ret.Append(vid + " [gms_id:" + _coordinatorGmsId +"] " + Global.CollectionToString(_members));
			return ret.ToString();
		}

		#region ICompactSerializable Members

		public virtual void Deserialize(CompactReader reader)
		{
			vid = (ViewId) reader.ReadObject();
			_members = (ArrayList) reader.ReadObject();
			_sequencerTbl = (Hashtable)reader.ReadObject();
			_mbrsSubgroupMap = (Hashtable)reader.ReadObject();
            _distributionMaps = (DistributionMaps)reader.ReadObject();
            _mirrorMapping = reader.ReadObject() as CacheNode[];
            nodeGmsIds = reader.ReadObject() as Hashtable;
            _coordinatorGmsId = reader.ReadObject() as string;
		}

		public virtual void Serialize(CompactWriter writer)
		{
			writer.WriteObject(vid);
            writer.WriteObject(_members);
			writer.WriteObject(_sequencerTbl);
			writer.WriteObject(_mbrsSubgroupMap);
            writer.WriteObject(_distributionMaps);
            writer.WriteObject(_mirrorMapping);
            writer.WriteObject(nodeGmsIds);
            writer.WriteObject(_coordinatorGmsId);
		}

		#endregion

		public static View ReadView(CompactReader reader)
		{
			byte isNull = reader.ReadByte();
			if (isNull == 1)
				return null;
			View newView = new View();
			newView.Deserialize(reader);
			return newView;
		}

		public static void WriteView(CompactWriter writer, View v)
		{
			byte isNull = 1;
			if (v == null)
				writer.Write(isNull);
			else
			{
				isNull = 0;
				writer.Write(isNull);
				v.Serialize(writer);
			}
			return;
		}  		    
	}
}
