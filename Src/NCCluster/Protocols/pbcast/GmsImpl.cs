// $Id: GmsImpl.java,v 1.4 2004/09/03 12:28:04 belaban Exp $
using System;
using Alachisoft.NGroups;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Alachisoft.NGroups.Protocols.pbcast
{
	internal abstract class GmsImpl
	{
        protected string _uniqueId;
		internal GMS gms = null;
		internal bool leaving = false;
		public abstract void  join(Address mbr, bool isStartedAsMirror);
		public abstract void  leave(Address mbr);
		
		public abstract void  handleJoinResponse(JoinRsp join_rsp);
		public abstract void  handleLeaveResponse();
		
		public abstract void  suspect(Address mbr);
		public abstract void  unsuspect(Address mbr);
		
		public virtual void  merge(System.Collections.ArrayList other_coords)
		{
			;
		} // only processed by coord
		public virtual void  handleMergeRequest(Address sender, object merge_id)
		{
			;
		} // only processed by coords
		public virtual void  handleMergeResponse(MergeData data, object merge_id)
		{
			;
		} // only processed by coords
		public virtual void  handleMergeView(MergeData data, object merge_id)
		{
			;
		} // only processed by coords
		public virtual void  handleMergeCancelled(object merge_id)
		{
			;
		} // only processed by coords

        public virtual void handleNotifyLeaving()
        {
            ;
        } // only processed by participants


        public abstract JoinRsp handleJoin(Address mbr, string subGroup_name, bool isStartedAsMirror, string gmsId);
		public abstract void  handleLeave(Address mbr, bool suspected);
		public abstract void  handleViewChange(View new_view, Digest digest);
		public abstract void  handleSuspect(Address mbr);
        public virtual void handleInformAboutNodeDeath(Address sender, Address deadNode) { }
        public virtual bool isInStateTransfer { get { return gms.GetStateTransferStatus(); } }

        public virtual void handleIsClusterInStateTransfer(Address sender)
        {
            Message msg = new Message(sender, null, new byte[0]);
            GMS.HDR hdr = new GMS.HDR(GMS.HDR.IS_NODE_IN_STATE_TRANSFER_RSP);
            gms.Stack.NCacheLog.Debug("gmsImpl.handleIsClusterInStateTransfer", "(state transfer request) sender: " + sender + " ->" + isInStateTransfer);
            hdr.arg = isInStateTransfer;
            msg.putHeader(HeaderType.GMS,hdr);
            gms.passDown(new Event(Event.MSG,msg,Priority.High));
        }

        /// <summary>
        /// A unique identifier that is shared by all the nodes participating in cluster.
        /// Upper cache layer uses this unique identifier when connected to the bridge as 
        /// a source cache.
        /// </summary>
        public virtual string UniqueId
        {
            get { return _uniqueId; }
            set
            {
                if (String.IsNullOrEmpty(_uniqueId))
                    _uniqueId = value;
            }
        }
      
        public virtual bool handleUpEvent(Event evt)
		{
			return true;
		}
		public virtual bool handleDownEvent(Event evt)
		{
			return true;
		}
		
		public virtual void  init()
		{
			leaving = false;
		}
		public virtual void  start()
		{
			leaving = false;
		}
		public virtual void  stop()
		{
			leaving = true;
		}

        public virtual void handleConnectionFailure(System.Collections.ArrayList nodes)
        {
        }
        public virtual void handleNodeRejoining(Address node)
        {
        }

        public virtual void handleInformNodeRejoining(Address sender, Address node)
        {

        }

        public virtual void handleResetOnNodeRejoining(Address sender, Address node,View view)
        {

        }

        public virtual void handleCanNotConnectTo(Address src,System.Collections.ArrayList failedNode)
        {
        }

        public virtual void handleLeaveClusterRequest(Address sender)
        {
        }

        public virtual void handleConnectedNodesRequest(Address sender,int reqid)
        {
            
        }
        public virtual void handleConnectedNodesResponse(Address sender, int reqid)
        {

        }

        public virtual void handleConnectionBroken(Address informer, Address suspected)
        {
        }

        public virtual void handleViewRejected(Address mbrRejected)
        {
        }
		protected internal virtual void  wrongMethod(string method_name)
		{
            if (gms.Stack.NCacheLog.IsInfoEnabled) gms.Stack.NCacheLog.Info(method_name + "() should not be invoked on an instance of " + GetType().FullName);
		}
			
		/// <summary>Returns potential coordinator based on lexicographic ordering of member addresses. Another
		/// approach would be to keep track of the primary partition and return the first member if we
		/// are the primary partition.
		/// </summary>
		protected internal virtual bool iWouldBeCoordinator(System.Collections.ArrayList new_mbrs)
		{
			Membership tmp_mbrs = gms.members.copy();
			tmp_mbrs.merge(new_mbrs, null);
			tmp_mbrs.sort();
			if (tmp_mbrs.size() <= 0 || gms.local_addr == null)
				return false;
			return gms.local_addr.Equals(tmp_mbrs.elementAt(0));
        }

        internal virtual void ReCheckClusterHealth(object mbr)
        {
            wrongMethod("ReCheckClusterHealth()");
        }

      
    }
}
