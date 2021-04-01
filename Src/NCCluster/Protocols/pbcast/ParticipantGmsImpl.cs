// $Id: ParticipantGmsImpl.java,v 1.7 2004/07/28 22:46:59 belaban Exp $
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Threading;
using System;
using System.Collections;

namespace Alachisoft.NGroups.Protocols.pbcast
{


    internal class ParticipantGmsImpl:GmsImpl
	{
		internal System.Collections.ArrayList suspected_mbrs = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(11));
		internal Promise leave_promise = new Promise();
        bool isNewMember = true;

        internal ArrayList initial_mbrs = ArrayList.Synchronized(new ArrayList(11));
        internal bool initial_mbrs_received = false;
        internal Promise join_promise = new Promise();
        private bool _deathVerificationInProgress;
        private object _deathVerSyncLock = new object();
		
		
		public ParticipantGmsImpl(GMS g)
		{
			gms = g;
			suspected_mbrs.Clear();
		}

        public override void handleNotifyLeaving()
        {

            leaving = true;
           
        }

		
		public override void  join(Address mbr, bool isStartedAsMirror)
		{
			wrongMethod("join");
		}
		
		
		/// <summary> Loop: determine coord. If coord is me --> handleLeave().
		/// Else send handleLeave() to coord until success
		/// </summary>
		public override void  leave(Address mbr)
		{
			Address coord;
			int max_tries = 3;
			object result;
			
			leave_promise.Reset();
			
			if (mbr.Equals(gms.local_addr))
				leaving = true;
			
			while ((coord = gms.determineCoordinator()) != null && max_tries-- > 0)
			{
				if (gms.local_addr.Equals(coord))
				{
					// I'm the coordinator
                    gms.becomeCoordinator();
					gms.Impl.handleLeave(mbr, false); // regular leave
					return ;
				}

                gms.Stack.NCacheLog.Debug("sending LEAVE request to " + coord);
				
				sendLeaveMessage(coord, mbr);
				lock (leave_promise)
				{
					result = leave_promise.WaitResult(gms.leave_timeout);
					if (result != null)
						break;
				}
			}
			gms.becomeClient();
		}
		
		
		public override void  handleJoinResponse(JoinRsp join_rsp)
		{
            join_promise.SetResult(join_rsp); // will wake up join() method
            gms.Stack.NCacheLog.CriticalInfo("CoordGMSImpl.handleJoin called at startup");
			wrongMethod("handleJoinResponse");
		}
		
		public override void  handleLeaveResponse()
		{
			if (leave_promise == null)
			{
                gms.Stack.NCacheLog.Error("ParticipantGmsImpl.handleLeaveResponse", "leave_promise is null.");
				return ;
			}
			lock (leave_promise)
			{
				leave_promise.SetResult((object) true); // unblocks thread waiting in leave()
			}
		}
		
		
		public override void  suspect(Address mbr)
		{
			handleSuspect(mbr);
		}
		
		
		/// <summary>Removes previously suspected member from list of currently suspected members </summary>
		public override void  unsuspect(Address mbr)
		{
			if (mbr != null)
				suspected_mbrs.Remove(mbr);
		}


        public override JoinRsp handleJoin(Address mbr, string subGroup_name, bool isStartedAsMirror, string gmsId)
		{
			wrongMethod("handleJoin");
			return null;
		}
		
		
		public override void  handleLeave(Address mbr, bool suspected)
		{
			wrongMethod("handleLeave");
		}
		
		
		/// <summary> If we are leaving, we have to wait for the view change (last msg in the current view) that
		/// excludes us before we can leave.
		/// </summary>
		/// <param name="new_view">The view to be installed
		/// </param>
		/// <param name="digest">  If view is a MergeView, digest contains the seqno digest of all members and has to
		/// be set by GMS
		/// </param>
		public override void  handleViewChange(View new_view, Digest digest)
		{
            if (gms.Stack.NCacheLog.IsInfoEnabled) gms.Stack.NCacheLog.Info("ParticipentGMSImpl.handleViewChange", "received view");
			System.Collections.ArrayList mbrs = new_view.Members;
            gms.Stack.NCacheLog.Debug("view=");// + new_view);
			suspected_mbrs.Clear();
			if (leaving && !mbrs.Contains(gms.local_addr))
			{
				// received a view in which I'm not member: ignore
				return ;
			}
			
			ViewId vid = gms.view_id != null ? gms.view_id.Copy() : null;
            if (vid != null)
            {
                int rc = vid.CompareTo(new_view.Vid);
                if (rc < 0)
                {
                    isNewMember = false;
                    if (gms.Stack.NCacheLog.IsInfoEnabled) gms.Stack.NCacheLog.Info("ParticipantGmsImp", "isNewMember : " + isNewMember);
                }
            }
           	gms.installView(new_view, digest);
		}

        public override void handleInformAboutNodeDeath(Address sender, Address deadNode)
        {
            lock (_deathVerSyncLock)
            {
                if (_deathVerificationInProgress)
                {
                    if (gms.Stack.NCacheLog.IsErrorEnabled) gms.Stack.NCacheLog.CriticalInfo("ParticipantGmsImp.handleInformAboutNodeDeath", "verification already in progress");
                    return;
                }
                else _deathVerificationInProgress = true;
            }

            Hashtable nodeGMSIds = new Hashtable();

            try
            {
                ArrayList suspectedMembers = new ArrayList();

                if (gms.Stack.NCacheLog.IsErrorEnabled) gms.Stack.NCacheLog.CriticalInfo("ParticipantGmsImp.handleInformAboutNodeDeath", sender + " reported node " + deadNode + " as down");
                
                if (gms.members.contains(deadNode))
                {
                    ArrayList members = gms.members.Members;

                    for (int i = 0; i < members.Count; i++)
                    {
                        Address mbr = members[i] as Address;
                        string gmsId = gms.GetNodeGMSId(mbr);
                        if (gmsId != null)
                            nodeGMSIds.Add(mbr, gmsId);
                    }

                    //Verify connectivity of over all cluster;
                    for (int i = 0; i < members.Count; i++)
                    {
                        Address mbr = members[i] as Address;

                        if (mbr.Equals(gms.local_addr))
                            break;

                        Address sameNode = null;

                        if (gms.isPartReplica)
                        {
                            Membership mbrShip = gms.members.copy();

                            if (mbrShip.contains(mbr))
                            {
                                for (int j = 0; j < mbrShip.size(); j++)
                                {
                                    Address other = mbrShip.elementAt(j);
                                    if (other != null && !other.Equals(mbr))
                                    {
                                        if (other.IpAddress.Equals(mbr.IpAddress))
                                        {
                                            sameNode = other;
                                            break;
                                        }
                                    }
                                }
                            }

                            //In case of POR if any other of either main/replica is veriifed dead, we consider the other one to be dead as well.
                            if (sameNode != null && suspectedMembers.Contains(sameNode)&& !suspectedMembers.Contains(mbr))
                            {
                                suspectedMembers.Add(mbr);
                                continue;
                            }
                        }

                        

                        if (gms.VerifySuspect(mbr))
                        {
                            if (gms.Stack.NCacheLog.IsErrorEnabled) gms.Stack.NCacheLog.CriticalInfo("ParticipantGmsImp.handleInformAboutNodeDeath", "verification of member down for " + mbr);
                            suspectedMembers.Add(mbr);
                        }
                        else
                        {
                            if (gms.Stack.NCacheLog.IsErrorEnabled) gms.Stack.NCacheLog.CriticalInfo("ParticipantGmsImp.handleInformAboutNodeDeath", mbr + " is up and running");
                            break;
                        }
                    }
                }
                else
                {
                    if (gms.Stack.NCacheLog.IsErrorEnabled) gms.Stack.NCacheLog.CriticalInfo("ParticipantGmsImp.handleInformAboutNodeDeath", "node is not part of members ");
                }

                foreach (Address suspectedMbr in suspectedMembers)
                {
                    string currentGmsId = gms.GetNodeGMSId(suspectedMbr);
                    string reportedGmsId = nodeGMSIds[suspectedMbr] as string;
                 
                    if(currentGmsId != null && reportedGmsId != null && currentGmsId.Equals(reportedGmsId))
                    {
                        handleSuspect(suspectedMbr);
                    }
                }
            }
            finally
            {
                lock (_deathVerSyncLock)
                {
                   _deathVerificationInProgress = false;
                }
            }
            
        }

		public override void  handleSuspect(Address mbr)
		{
			System.Collections.ArrayList suspects = null;

            lock (this)
            {
                if (mbr == null)
                    return;
                if (!suspected_mbrs.Contains(mbr))
                {
                    Address sameNode = null;

                    if (gms.isPartReplica)
                    {
                        Membership mbrShip = gms.members.copy();

                        if (mbrShip.contains(mbr))
                        {
                            for (int i = 0; i < mbrShip.size(); i++)
                            {
                                Address other = mbrShip.elementAt(i);
                                if (other != null && !other.Equals(mbr))
                                {
                                    if (other.IpAddress.Equals(mbr.IpAddress))
                                    {
                                        sameNode = other;
                                        break;
                                    }
                                }
                            }
                        }
                    }


                    if (sameNode != null && !sameNode.IpAddress.Equals(gms.local_addr.IpAddress))
                    {
                        if (sameNode.Port > mbr.Port)
                        {
                            suspected_mbrs.Add(sameNode);
                            suspected_mbrs.Add(mbr);
                        }
                        else
                        {
                            suspected_mbrs.Add(mbr);
                            suspected_mbrs.Add(sameNode);
                        }
                    }
                    else
                    {
                        suspected_mbrs.Add(mbr);
                    }
                }



                if (gms.Stack.NCacheLog.IsInfoEnabled) gms.Stack.NCacheLog.Info("suspected mbr=" + mbr + ", suspected_mbrs=" + Global.CollectionToString(suspected_mbrs));

                if (!leaving)
                {
                    if (wouldIBeCoordinator() && !gms.IsCoordinator)
                    {
                        suspects = (System.Collections.ArrayList)suspected_mbrs.Clone();
                        suspected_mbrs.Clear();
                        gms.becomeCoordinator();

                        foreach (Address leavingMbr in suspects)
                        {
                            if (!gms.members.Members.Contains(leavingMbr))
                            {
                                gms.Stack.NCacheLog.Debug("pbcast.PariticipantGmsImpl.handleSuspect()", "mbr " + leavingMbr + " is not a member !");
                                continue;
                            }
                            if (gms.Stack.NCacheLog.IsInfoEnabled) gms.Stack.NCacheLog.Info("suspected mbr=" + leavingMbr + "), members are " + gms.members + ", coord=" + gms.local_addr + ": I'm the new coord !");
                            //=====================================================
                            //update gms' subgroupMbrMap.
                            string subGroup = (string)gms._mbrSubGroupMap[leavingMbr];
                            if (subGroup != null)
                            {
                                lock (gms._mbrSubGroupMap.SyncRoot)
                                {
                                    gms._mbrSubGroupMap.Remove(leavingMbr);
                                }
                                lock (gms._subGroupMbrsMap.SyncRoot)
                                {
                                    System.Collections.ArrayList subGroupMbrs = (System.Collections.ArrayList)gms._subGroupMbrsMap[subGroup];
                                    if (subGroupMbrs != null)
                                    {
                                        subGroupMbrs.Remove(leavingMbr);
                                        if (subGroupMbrs.Count == 0)
                                        {
                                            gms._subGroupMbrsMap.Remove(subGroup);
                                        }
                                    }
                                }
                            }
                            //=====================================================
                            ArrayList list = new ArrayList(1);
                            list.Add(leavingMbr);
                           
                        }
                        gms.castViewChange(null, null, suspects, gms._hashmap);
                    }
                    else
                    {
                        if(gms.IsCoordinator)
                            sendMemberLeftNotificationToCoordinator(mbr, gms.local_addr);
                        else
                            sendMemberLeftNotificationToCoordinator(mbr,gms.determineCoordinator());
                    }
                }
            }
		}

        private void sendMemberLeftNotificationToCoordinator(Address suspected,Address coordinator)
        {
            if (gms.Stack.NCacheLog.IsInfoEnabled) gms.Stack.NCacheLog.Info("ParticipantGmsImp.sendMemberLeftNotification", "informing coordinator about abnormal connection breakage with " + suspected);

            GMS.HDR hdr = new GMS.HDR(GMS.HDR.CONNECTION_BROKEN, suspected);
            Message nodeLeftMsg = new Message(coordinator, null, new byte[0]);
            nodeLeftMsg.putHeader(HeaderType.GMS, hdr);
            gms.passDown(new Event(Event.MSG, nodeLeftMsg, Priority.High));
        }

        public override void handleConnectedNodesRequest(Address src,int reqId)
        {
            if (gms.determineCoordinator().Equals(src))
            {
                ArrayList mbrs = gms.members.Members;
                ArrayList suspected = suspected_mbrs.Clone() as ArrayList;

                foreach (Address suspect in suspected_mbrs)
                {
                    mbrs.Remove(suspect);
                }

                if (gms.Stack.NCacheLog.IsInfoEnabled) gms.Stack.NCacheLog.Info("ParticipantGmsImp.handleConnectedNodesRequest    " + gms.local_addr + " --> " + Global.ArrayListToString(mbrs));

                Message rspMsg = new Message(src,null,new byte[0]);
                GMS.HDR hdr = new GMS.HDR(GMS.HDR.CONNECTED_NODES_RESPONSE,(Object)reqId);
                hdr.nodeList = mbrs;
                rspMsg.putHeader(HeaderType.GMS,hdr);
                gms.passDown(new Event(Event.MSG,rspMsg,Priority.High));
            }
        }
        /// <summary>
        /// Informs the coodinator about the nodes to which this node can not establish connection
        /// on receiving the first view.Only the node who has most recently joined the cluster
        /// should inform the coodinator other nodes will neglect this event.
        /// </summary>
        /// <param name="nodes"></param>
        public override void handleConnectionFailure(System.Collections.ArrayList nodes)
        {
            if (nodes != null && nodes.Count > 0)
            {
                if (gms.Stack.NCacheLog.IsInfoEnabled) gms.Stack.NCacheLog.Info("ParticipantGmsImp.handleConnectionFailure", "informing coordinator about connection failure with [" + Global.CollectionToString(nodes) + "]");
                GMS.HDR header = new GMS.HDR(GMS.HDR.CAN_NOT_CONNECT_TO);
                header.nodeList = nodes;
                Message msg = new Message(gms.determineCoordinator(),null,new byte[0]);
                msg.putHeader(HeaderType.GMS,header);
                gms.passDown(new Event(Event.MSG,msg,Priority.High));
            }
            
        }

        public override void handleLeaveClusterRequest(Address sender)
        {
            if (gms.Stack.NCacheLog.IsInfoEnabled) gms.Stack.NCacheLog.Error("ParticipantGmsImp.handleLeaveClusterRequest", sender + " has asked me to leave the cluster");

            if (gms.determineCoordinator().Equals(sender))
            {
                if (gms.Stack.NCacheLog.IsInfoEnabled) gms.Stack.NCacheLog.Error("ParticipantGmsImp.handleLeaveClusterRequest", "leaving the cluster on coordinator request");

                ArrayList suspected = gms.members.Members;
                suspected.Remove(gms.local_addr);

                if (gms.isPartReplica)
                {
                    //In leave cluster scenario, we remove all other nodes except replica residing on the physical node
                    suspected.Remove(new Address(gms.local_addr.IpAddress, gms.local_addr.Port + 1));
                }

                foreach (Address leavingMbr in suspected)
                {
                    string subGroup = gms._mbrSubGroupMap[leavingMbr] as string;
                    
                    if (subGroup != null)
                    {
                        lock (gms._mbrSubGroupMap.SyncRoot)
                        {
                            gms._mbrSubGroupMap.Remove(leavingMbr);
                        }

                        lock (gms._subGroupMbrsMap.SyncRoot)
                        {
                            ArrayList subGroupMbrs = gms._subGroupMbrsMap[subGroup] as ArrayList;
                            if (subGroupMbrs != null)
                            {
                                subGroupMbrs.Remove(leavingMbr);
                                if (subGroupMbrs.Count == 0)
                                {
                                    gms._subGroupMbrsMap.Remove(subGroup);
                                }
                            }
                        }
                    }

                    ArrayList list = new ArrayList(1);
                    list.Add(leavingMbr);
        
                }

                suspected_mbrs.Clear();
                gms.becomeCoordinator();
                gms.castViewChange(null, null, suspected, gms._hashmap);
            }
            
        }

        public override void handleNodeRejoining(Address node)
        {
            if (node != null)
            {
                if (gms.Stack.NCacheLog.IsInfoEnabled) gms.Stack.NCacheLog.Info("ParticipantGmsImpl.handleNodeRejoining", "I should inform coordinator about node rejoining with " + node);

                if (gms.members.contains(node))
                {
                    //inform coordinator about the node rejoining in the cluster.
                    GMS.HDR header = new GMS.HDR(GMS.HDR.INFORM_NODE_REJOINING, node);
                    Message rejoiningMsg = new Message(gms.determineCoordinator(), null, new byte[0]);
                    rejoiningMsg.putHeader(HeaderType.GMS, header);
                    gms.passDown(new Event(Event.MSG, rejoiningMsg, Priority.High));
                }
            }
        }
        public override void handleResetOnNodeRejoining(Address sender, Address node, View view)
        {
            gms.handleResetOnNodeRejoining(sender, node,view);
        }
		/* ---------------------------------- Private Methods --------------------------------------- */
		
		/// <summary> Determines whether this member is the new coordinator given a list of suspected members.  This is
		/// computed as follows: the list of currently suspected members (suspected_mbrs) is removed from the current
		/// membership. If the first member of the resulting list is equals to the local_addr, then it is true,
		/// otherwise false. Example: own address is B, current membership is {A, B, C, D}, suspected members are {A,
		/// D}. The resulting list is {B, C}. The first member of {B, C} is B, which is equal to the
		/// local_addr. Therefore, true is returned.
		/// </summary>
		internal virtual bool wouldIBeCoordinator()
		{
			Address new_coord = null;
			System.Collections.ArrayList mbrs = gms.members.Members; // getMembers() returns a *copy* of the membership vector
			
			for (int i = 0; i < suspected_mbrs.Count; i++)
				mbrs.Remove(suspected_mbrs[i]);
			
			if (mbrs.Count < 1)
				return false;
			new_coord = (Address) mbrs[0];
			return gms.local_addr.Equals(new_coord);
		}
		
		
		internal virtual void  sendLeaveMessage(Address coord, Address mbr)
		{
			Message msg = new Message(coord, null, null);
			GMS.HDR hdr = new GMS.HDR(GMS.HDR.LEAVE_REQ, mbr);

            msg.putHeader(HeaderType.GMS, hdr);
			gms.passDown(new Event(Event.MSG, msg));
        }

		/* ------------------------------ End of Private Methods ------------------------------------ */


        internal override void ReCheckClusterHealth(object mbr)
        {
            JoinRsp rsp;
            Digest tmp_digest;
            Address coordinator = (Address)mbr;

            gms.Stack.NCacheLog.Debug("ReCheck", "Force join cluster: " + mbr.ToString());
            int retry_count = 6;

            try
            {
                while (!leaving)
                {
                    if (coordinator != null && !coordinator.Equals(gms.local_addr))
                    {
                        sendJoinMessage(coordinator, gms.local_addr, gms.subGroup_addr, true);
                        rsp = (JoinRsp)join_promise.WaitResult(gms.join_timeout * 5);
                    }
                    else
                    {
                        retry_count--;
                        if (retry_count <= 0)
                            return;
                        continue;
                    }
                    if (rsp == null)
                    {
                        Util.Util.sleep(gms.join_retry_timeout * 5);
                        initial_mbrs_received = false;
                        retry_count--;
                        if (retry_count <= 0)
                            return;
                        continue;
                    }
                    else if (rsp.JoinResult == JoinResult.Rejected)
                        return;
                    else if (rsp.JoinResult == JoinResult.MembershipChangeAlreadyInProgress)
                    {
                        Util.Util.sleep(gms.join_timeout);
                        continue;
                    }
                    else
                    {
                        tmp_digest = rsp.Digest;
                        if (tmp_digest != null)
                        {
                            tmp_digest.incrementHighSeqno(coordinator); // see DESIGN for an explanantion
                            gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.join()", "digest is " + tmp_digest);
                            gms.Digest = tmp_digest;
                        }
                        else
                            gms.Stack.NCacheLog.Error("pb.ClientGmsImpl.join()", "digest of JOIN response is null");

                        // 2. Install view
                        gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.join()", "[" + gms.local_addr + "]: JoinRsp=" + rsp.View + " [size=" + rsp.View.size() + "]\n\n");

                        if (rsp.View != null)
                        {
                            rsp.View.ForceInstall = true; //Forces this view for installation
                            if (!installView(rsp.View))
                            {
                                gms.Stack.NCacheLog.Error("pb.ClientGmsImpl.join()", "view installation failed, retrying to join group");
                                return;
                            }
                            gms.Stack.IsOperational = true;
                            return;
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                gms.Stack.NCacheLog.Error("pb.ClientGmsImpl.join()", ex.ToString());
            }

        }

        private bool installView(View new_view)
        {
            ArrayList mems = new_view.Members;
            gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.installView()", "new_view=" + new_view);
            if (gms.local_addr == null || mems == null || !mems.Contains(gms.local_addr))
            {
                gms.Stack.NCacheLog.Error("pb.ClientGmsImpl.installView()", "I (" + gms.local_addr + ") am not member of " + Global.CollectionToString(mems) + ", will not install view");
                return false;
            }

            Address replica = (Address)gms.members.Members[1];


            //Cast view to the replica node as well
            gms.installView(new_view);
            //gms.castViewChange(new_view);

            gms.becomeParticipant();
            gms.Stack.IsOperational = true;
            return true;
        }

        private Address determineCoord(ArrayList mbrs)
        {
            if (mbrs == null || mbrs.Count < 1)
                return null;

            Address winner = null;
            int max_votecast = 0;
            Hashtable votes = Hashtable.Synchronized(new Hashtable(11));
            for (int i = 0; i < mbrs.Count; i++)
            {
                PingRsp mbr = (PingRsp)mbrs[i];
                if (mbr.CoordAddress != null)
                {
                    if (!votes.ContainsKey(mbr.CoordAddress))
                        votes[mbr.CoordAddress] = mbr.HasJoined ? 1000 : 1;
                    else
                    {
                        int count = ((int)votes[mbr.CoordAddress]);
                        votes[mbr.CoordAddress] = (int)(count + 1);
                    }

                    /// Find the maximum vote cast value. This will be used to resolve a
                    /// tie later on. (shoaib)
                    if (((int)votes[mbr.CoordAddress]) > max_votecast)
                        max_votecast = ((int)votes[mbr.CoordAddress]);
                    if ((mbr.OwnAddress.IpAddress.Equals(gms.local_addr.IpAddress)) && (mbr.OwnAddress.Port < gms.local_addr.Port))
                    {
                        gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.determineCoord()", "WINNER SET TO ACTIVE NODE's Coord = " + Convert.ToString(mbr.CoordAddress));
                        winner = mbr.CoordAddress;
                        break;
                    }
                }
            }

            if (winner == null)
                return null;
            else
                return winner;
        }

        /// <summary> Pings initial members. Removes self before returning vector of initial members.
        /// Uses IP multicast or gossiping, depending on parameters.
        /// </summary>
        internal ArrayList FindAliveMembers()
        {
            PingRsp ping_rsp;
            initial_mbrs.Clear();
            initial_mbrs_received = false;
            gms.passDown(new Event(Event.FIND_INITIAL_MBRS));

            // the initial_mbrs_received flag is needed when passDown() is executed on the same thread, so when
            // it returns, a response might actually have been received (even though the initial_mbrs might still be empty)
            if (initial_mbrs_received == false)
            {
                try
                {
                    System.Threading.Monitor.Wait(initial_mbrs.SyncRoot);
                }
                catch (System.Exception ex)
                {
                    gms.Stack.NCacheLog.Error("COORDGmsImpl.findInitialMembers", ex.Message);
                }
            }

            for (int i = 0; i < initial_mbrs.Count; i++)
            {
                ping_rsp = (PingRsp)initial_mbrs[i];
                if (ping_rsp.OwnAddress != null && gms.local_addr != null && ping_rsp.OwnAddress.Equals(gms.local_addr))
                {
                    //initial_mbrs.RemoveAt(i);
                    break;
                }
                if (!ping_rsp.IsStarted) initial_mbrs.RemoveAt(i);
            }

            return initial_mbrs;
        }

        public override bool handleUpEvent(Event evt)
        {
            ArrayList tmp;

            switch (evt.Type)
            {
                case Event.FIND_INITIAL_MBRS_OK:
                    tmp = (ArrayList)evt.Arg;
                    lock (initial_mbrs.SyncRoot)
                    {
                        if (tmp != null && tmp.Count > 0)
                            for (int i = 0; i < tmp.Count; i++)
                                initial_mbrs.Add(tmp[i]);
                        initial_mbrs_received = true;
                        System.Threading.Monitor.Pulse(initial_mbrs.SyncRoot);
                    }
                    return false; // don't pass up the stack
            }
            return true;
        }

        internal virtual void sendJoinMessage(Address coord, Address mbr, string subGroup_name, bool isStartedAsMirror)
        {
            Message msg;
            GMS.HDR hdr;

            msg = new Message(coord, null, null);
            hdr = new GMS.HDR(GMS.HDR.JOIN_REQ, mbr, subGroup_name, isStartedAsMirror);
            hdr.GMSId = gms.unique_id;
            msg.putHeader(HeaderType.GMS, hdr);
            gms.passDown(new Event(Event.MSG_URGENT, msg, Priority.High));
        }


	}
}
