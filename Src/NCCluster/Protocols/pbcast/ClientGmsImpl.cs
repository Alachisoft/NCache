// $Id: ClientGmsImpl.java,v 1.12 2004/09/08 09:17:17 belaban Exp $
using System;
using System.Collections;

using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NGroups.Protocols.pbcast
{
    /// <summary> Client part of GMS. Whenever a new member wants to join a group, it starts in the CLIENT role.
    /// No multicasts to the group will be received and processed until the member has been joined and
    /// turned into a SERVER (either coordinator or participant, mostly just participant). This class
    /// only implements <code>Join</code> (called by clients who want to join a certain group, and
    /// <code>ViewChange</code> which is called by the coordinator that was contacted by this client, to
    /// tell the client what its initial membership is.
    /// </summary>
    /// <author>  Bela Ban
    /// </author>
    /// <version>  $Revision: 1.12 $
    /// </version>
    internal class ClientGmsImpl:GmsImpl
	{
		internal ArrayList initial_mbrs = ArrayList.Synchronized(new ArrayList(11));
		internal bool initial_mbrs_received = false;
		internal object view_installation_mutex = new object();
		internal Promise join_promise = new Promise();
		
		public ClientGmsImpl(GMS g)
		{
			gms = g;
		}
		
		
		/// <summary> Joins this process to a group. Determines the coordinator and sends a unicast
		/// handleJoin() message to it. The coordinator returns a JoinRsp and then broadcasts the new view, which
		/// contains a message digest and the current membership (including the joiner). The joiner is then
		/// supposed to install the new view and the digest and starts accepting mcast messages. Previous
		/// mcast messages were discarded (this is done in PBCAST).<p>
		/// If successful, impl is changed to an instance of ParticipantGmsImpl.
		/// Otherwise, we continue trying to send join() messages to	the coordinator,
		/// until we succeed (or there is no member in the group. In this case, we create our own singleton group).
		/// <p>When GMS.disable_initial_coord is set to true, then we won't become coordinator on receiving an initial
		/// membership of 0, but instead will retry (forever) until we get an initial membership of > 0.
		/// </summary>
		/// <param name="mbr">Our own address (assigned through SET_LOCAL_ADDRESS)
		/// </param>
		public override void  join(Address mbr, bool isStartedAsMirror)
		{
			Address coord = null;
            Address last_tried_coord = null;
			JoinRsp rsp = null;
			Digest tmp_digest = null;
			leaving = false;
            int join_retries = 1;

			join_promise.Reset();
			while (!leaving)
			{
				findInitialMembers();
               
				gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.join()",   "initial_mbrs are " + Global.CollectionToString(initial_mbrs));
				if (initial_mbrs.Count == 0)
				{
					if (gms.disable_initial_coord)
					{
                        gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.join()",   "received an initial membership of 0, but cannot become coordinator (disable_initial_coord=" + gms.disable_initial_coord + "), will retry fetching the initial membership");
						continue;
					}
                    gms.Stack.NCacheLog.CriticalInfo("ClientGmsImpl.Join",   "no initial members discovered: creating group as first member.");

                    becomeSingletonMember(mbr);
					return ;
				}
				
				coord = determineCoord(initial_mbrs);
				if (coord == null)
				{
                    gms.Stack.NCacheLog.Error("pb.ClientGmsImpl.join()",   "could not determine coordinator from responses " + Global.CollectionToString(initial_mbrs));
					continue;
				}
				if (coord.CompareTo(gms.local_addr) == 0)
				{
                    gms.Stack.NCacheLog.Error("pb.ClientGmsImpl.join()",   "coordinator anomaly. More members exist yet i am the coordinator " + Global.CollectionToString(initial_mbrs));
                    ArrayList members = new ArrayList();
                    for (int i = 0; i < initial_mbrs.Count; i++)
                    {
                        PingRsp ping_rsp = (PingRsp)initial_mbrs[i];
                        if (ping_rsp.OwnAddress != null && gms.local_addr != null && !ping_rsp.OwnAddress.Equals(gms.local_addr))
                        {
                            members.Add(ping_rsp.OwnAddress);
                        }
                    }
                    gms.InformOthersAboutCoordinatorDeath(members, coord);
                    if (last_tried_coord == null)
                        last_tried_coord = coord;
                    else
                    {
                        if (last_tried_coord.Equals(coord))
                            join_retries++;
                        else
                        {
                            last_tried_coord = coord;
                            join_retries = 1;
                        }
                    }

                    Util.Util.sleep(gms.join_timeout);
                    continue;
				}
				
				try
				{
                    gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.join()",   "sending handleJoin(" + mbr + ") to " + coord);
                   
                    if (last_tried_coord == null)
                        last_tried_coord = coord;
                    else
                    {
                        if (last_tried_coord.Equals(coord))
                            join_retries++;
                        else
                        {
                            last_tried_coord = coord;
                            join_retries = 1;
                        }
                    }

					sendJoinMessage(coord, mbr, gms.subGroup_addr, isStartedAsMirror);
					rsp = (JoinRsp) join_promise.WaitResult(gms.join_timeout);

                    if (rsp == null)
					{
                        if (join_retries >= gms.join_retry_count)
                        {
                            gms.Stack.NCacheLog.Error("ClientGmsImpl.Join", "received no joining response after " + join_retries + " tries, so becoming a singlton member");
                            becomeSingletonMember(mbr);
                            return;
                        }
                        else
                        {
                            //I did not receive join response, so there is a chance that coordinator is down
                            //Lets verifiy it.
                            if (gms.VerifySuspect(coord,false))
                            {
                                if(gms.Stack.NCacheLog.IsErrorEnabled) gms.Stack.NCacheLog.CriticalInfo("ClientGmsImpl.Join()", "selected coordinator " + coord + " seems down; Lets inform others");
                                //Coordinator is not alive;Lets inform the others
                                ArrayList members = new ArrayList();
                                for (int i = 0; i < initial_mbrs.Count; i++)
                                {
                                    PingRsp ping_rsp = (PingRsp)initial_mbrs[i];
                                    
                                    if (ping_rsp.OwnAddress != null && gms.local_addr != null && !ping_rsp.OwnAddress.Equals(gms.local_addr))
                                    {
                                        members.Add(ping_rsp.OwnAddress);
                                    }
                                }
                                gms.InformOthersAboutCoordinatorDeath(members, coord);
                            }
                        }
                        gms.Stack.NCacheLog.Error("ClientGmsImpl.Join()",   "handleJoin(" + mbr + ") failed, retrying; coordinator:" + coord + " ;No of retries : " + (join_retries + 1));

					}
					else
					{
                        if (rsp.JoinResult == JoinResult.Rejected)
                        {
                            gms.Stack.NCacheLog.Error("ClientGmsImpl.Join",   "joining request rejected by coordinator");
                            becomeSingletonMember(mbr);
                            return;
                        }

                        if (rsp.JoinResult == JoinResult.MembershipChangeAlreadyInProgress)
                        {
                            gms.Stack.NCacheLog.CriticalInfo("Coord.CheckOwnClusterHealth", "Reply: JoinResult.MembershipChangeAlreadyInProgress");
                            Util.Util.sleep(gms.join_timeout);
                            continue;
                        }

                        gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.join()", "Join successfull");

						// 1. Install digest
						tmp_digest = rsp.Digest;
						if (tmp_digest != null)
						{
							tmp_digest.incrementHighSeqno(coord); // see DESIGN for an explanantion
                            gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.join()",   "digest is " + tmp_digest);
							gms.Digest = tmp_digest;
						}
						else
                            gms.Stack.NCacheLog.Error("pb.ClientGmsImpl.join()",   "digest of JOIN response is null");
						
						// 2. Install view
                        gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.join()",   "[" + gms.local_addr + "]: JoinRsp=" + rsp.View + " [size=" + rsp.View.size() + "]\n\n");
												
						if (rsp.View != null)
						{
                            
							if (!installView(rsp.View))
							{
                                gms.Stack.NCacheLog.Error("pb.ClientGmsImpl.join()",   "view installation failed, retrying to join group");
								continue;
							}
							gms.Stack.IsOperational = true;
							return ;
						}
						else
                            gms.Stack.NCacheLog.Error("pb.ClientGmsImpl.join()",   "view of JOIN response is null");
					}
				}
				catch (System.Exception e)
				{
                    gms.Stack.NCacheLog.Error("ClientGmsImpl.join()",   "Message: "+e.Message+"  StackTrace: "+e.StackTrace + ", retrying");					
				}
				
				Util.Util.sleep(gms.join_retry_timeout);
			}

		}
		
       
		
		public override void  leave(Address mbr)
		{
			leaving = true;
			wrongMethod("leave");
		}
		
		
		public override void  handleJoinResponse(JoinRsp join_rsp)
		{
			join_promise.SetResult(join_rsp); // will wake up join() method
		}
		
		public override void  handleLeaveResponse()
		{
		}
		
		
		public override void  suspect(Address mbr)
		{
			wrongMethod("suspect");
		}
		
		public override void  unsuspect(Address mbr)
		{
			wrongMethod("unsuspect");
		}


        public override JoinRsp handleJoin(Address mbr, string subGroup_name, bool isStartedAsMirror, string gmsId)
		{
			wrongMethod("handleJoin");
			return null;
		}
		
		
		/// <summary>Returns false. Clients don't handle leave() requests </summary>
		public override void  handleLeave(Address mbr, bool suspected)
		{
			wrongMethod("handleLeave");
		}
		
		
		/// <summary> Does nothing. Discards all views while still client.</summary>
		public override void  handleViewChange(View new_view, Digest digest)
		{
			lock (this)
			{
                gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.handleViewChange()",   "view " + Global.CollectionToString(new_view.Members) + " is discarded as we are not a participant");
			}
            gms.passDown(new Event(Event.VIEW_CHANGE_OK, new object(),Priority.High));
		}
		
		
		/// <summary> Called by join(). Installs the view returned by calling Coord.handleJoin() and
		/// becomes coordinator.
		/// </summary>
		private bool installView(View new_view)
		{
			ArrayList mems = new_view.Members;
            gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.installView()",   "new_view=" + new_view);
			if (gms.local_addr == null || mems == null || !mems.Contains(gms.local_addr))
			{
                gms.Stack.NCacheLog.Error("pb.ClientGmsImpl.installView()",   "I (" + gms.local_addr + ") am not member of " + Global.CollectionToString(mems) + ", will not install view");
				return false;
			}
			gms.installView(new_view);
			gms.becomeParticipant();
			gms.Stack.IsOperational = true;
			return true;
		}
		
		
		/// <summary>Returns immediately. Clients don't handle suspect() requests </summary>
		public override void  handleSuspect(Address mbr)
		{
			wrongMethod("handleSuspect");
			return ;
		}
        /// <summary>
        /// Informs the coodinator about the nodes to which this node can not establish connection
        /// on receiving the first view. Only the node who has most recently joined the cluster
        /// should inform the coodinator other nodes will neglect this event.
        /// </summary>
        /// <param name="nodes"></param>
        public override void handleConnectionFailure(System.Collections.ArrayList nodes)
        {
            if (nodes != null && nodes.Count > 0)
            {
                if (gms.Stack.NCacheLog.IsInfoEnabled) gms.Stack.NCacheLog.Info("ClientGmsImp.handleConnectionFailure", "informing coordinator about connection failure with [" + Global.CollectionToString(nodes) + "]");
                GMS.HDR header = new GMS.HDR(GMS.HDR.CAN_NOT_CONNECT_TO);
                header.nodeList = nodes;
                Message msg = new Message(gms.determineCoordinator(), null, new byte[0]);
                msg.putHeader(HeaderType.GMS, header);
                gms.passDown(new Event(Event.MSG, msg, Priority.High));
            }

        }
		
		public override bool handleUpEvent(Event evt)
		{
			ArrayList tmp;
			
			switch (evt.Type)
			{
				case Event.FIND_INITIAL_MBRS_OK: 
					tmp = (ArrayList) evt.Arg;
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
		
		
		/* --------------------------- Private Methods ------------------------------------ */
		
		
		
		internal virtual void  sendJoinMessage(Address coord, Address mbr, string subGroup_name, bool isStartedAsMirror)
		{
			Message msg;
			GMS.HDR hdr;

			msg = new Message(coord, null, null);
			hdr = new GMS.HDR(GMS.HDR.JOIN_REQ, mbr, subGroup_name, isStartedAsMirror);
            hdr.GMSId = gms.unique_id;
			msg.putHeader(HeaderType.GMS, hdr);
			gms.passDown(new Event(Event.MSG_URGENT, msg,Priority.High));
		}

        internal virtual void sendSpeicalJoinMessage(Address mbr, ArrayList dests)
        {
            Message msg;
            GMS.HDR hdr;

            msg = new Message(null, null, new byte[0]);
            msg.Dests = dests;
            hdr = new GMS.HDR(GMS.HDR.SPECIAL_JOIN_REQUEST, mbr);
            hdr.GMSId = gms.unique_id;
            msg.putHeader(HeaderType.GMS, hdr);
            gms.passDown(new Event(Event.MSG_URGENT, msg, Priority.High));
        }
		
		
		/// <summary> Pings initial members. Removes self before returning vector of initial members.
		/// Uses IP multicast or gossiping, depending on parameters.
		/// </summary>
		internal virtual void  findInitialMembers()
		{
			PingRsp ping_rsp;
			
			lock (initial_mbrs.SyncRoot)
			{
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
                        gms.Stack.NCacheLog.Error("ClientGmsImpl.findInitialMembers",   ex.Message);
					}
				}
				
				for (int i = 0; i < initial_mbrs.Count; i++)
				{
					ping_rsp = (PingRsp) initial_mbrs[i];
					if (ping_rsp.OwnAddress != null && gms.local_addr != null && ping_rsp.OwnAddress.Equals(gms.local_addr))
					{
						//initial_mbrs.RemoveAt(i);
						break;
					}
                    if (!ping_rsp.IsStarted) initial_mbrs.RemoveAt(i);
				}
			}
		}
		
		
		/// <summary>The coordinator is determined by a majority vote. 
		/// If there are an equal number of votes for more than 1 candidate, we determine the winner randomly.
		/// 
		/// This is bad!. I've changed the election process altogether. I guess i'm the new pervez musharaf here
		/// Let everyone cast a vote and unlike the non-deterministic coordinator selection process of jgroups. Ours
		/// is a deterministic one. First we find members with most vote counts. If there is a tie member with lowest
		/// IP addres wins, if there is tie again member with low port value wins. 
		/// 
		/// This algortihm is determistic and ensures same results on every node icluding the coordinator. (shoaib)
		/// </summary>
		internal virtual Address determineCoord(ArrayList mbrs)
		{
			if (mbrs == null || mbrs.Count < 1)
				return null;

            Address winner = null;
			int max_votecast = 0;
			Hashtable votes = Hashtable.Synchronized(new Hashtable(11));
			for (int i = 0; i < mbrs.Count; i++)
			{
				PingRsp mbr = (PingRsp) mbrs[i];
				if (mbr.CoordAddress != null)
				{
					if (!votes.ContainsKey(mbr.CoordAddress))
						votes[mbr.CoordAddress] = mbr.HasJoined ? 1000:1;
					else
					{
						int count = ((int) votes[mbr.CoordAddress]);
						votes[mbr.CoordAddress] = (int) (count + 1);
					}

					/// Find the maximum vote cast value. This will be used to resolve a
					/// tie later on. (shoaib)
					if(((int) votes[mbr.CoordAddress]) > max_votecast)
						max_votecast = ((int) votes[mbr.CoordAddress]);

                    gms.Stack.NCacheLog.CriticalInfo("pb.ClientGmsImpl.determineCoord()", "Owner " + mbr.OwnAddress + " -- CoordAddress " + mbr.CoordAddress + " -- Vote " + (int)votes[mbr.CoordAddress]);

                    if ((mbr.OwnAddress.IpAddress.Equals(gms.local_addr.IpAddress)) && (mbr.OwnAddress.Port < gms.local_addr.Port))
                    {
                        gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.determineCoord()", "WINNER SET TO ACTIVE NODE's Coord = " + Convert.ToString(mbr.CoordAddress));
                        winner = mbr.CoordAddress;
                    }
				}
			}

			

			/// Collect all the candidates with the highest but similar vote count.
			/// Ideally there should only be one. (shoaib)
			ArrayList candidates = new ArrayList(votes.Count);
			for (IDictionaryEnumerator e = votes.GetEnumerator(); e.MoveNext(); )
			{
				if (((int)e.Value) == max_votecast)
				{
					candidates.Add(e.Key);
				}
			}

			candidates.Sort();
            if (winner == null)
            {
                winner = (Address)candidates[0]; 
            }

			if (candidates.Count > 1)
                gms.Stack.NCacheLog.Warn("pb.ClientGmsImpl.determineCoord()",   "there was more than 1 candidate for coordinator: " + Global.CollectionToString(candidates));
            gms.Stack.NCacheLog.CriticalInfo("pb.ClientGmsImpl.determineCoord()",   "election winner: " + winner + " with votes " + max_votecast);


			return winner;
		}
		
		
		internal virtual void  becomeSingletonMember(Address mbr)
		{
			Digest initial_digest;
			ViewId view_id = null;
			ArrayList mbrs = ArrayList.Synchronized(new ArrayList(1));
			
			// set the initial digest (since I'm the first member)
			initial_digest = new Digest(1); // 1 member (it's only me)
			initial_digest.add(gms.local_addr, 0, 0); // initial seqno mcast by me will be 1 (highest seen +1)
			gms.Digest = initial_digest;
			
			view_id = new ViewId(mbr); // create singleton view with mbr as only member
			mbrs.Add(mbr);
			
			View v = new View(view_id, mbrs);
            v.CoordinatorGmsId = gms.unique_id;
			ArrayList subgroupMbrs = new ArrayList();
			subgroupMbrs.Add(mbr);
			gms._subGroupMbrsMap[gms.subGroup_addr] = subgroupMbrs;
			gms._mbrSubGroupMap[mbr] = gms.subGroup_addr;
			v.SequencerTbl = gms._subGroupMbrsMap.Clone() as Hashtable;
			v.MbrsSubgroupMap = gms._mbrSubGroupMap.Clone() as Hashtable;
            v.AddGmsId(mbr, gms.unique_id);
			gms.installView(v);
			gms.becomeCoordinator(); // not really necessary - installView() should do it
			
			gms.Stack.IsOperational = true;
            gms.Stack.NCacheLog.Debug("pb.ClientGmsImpl.becomeSingletonMember()",   "created group (first member). My view is " + gms.view_id + ", impl is " + gms.Impl.GetType().FullName);

		}

        /// <summary>
        /// In case of PartitionOfReplica this will verify that all the initial memebrs are completely up and running, It will also verify that 
        /// the replica of a POR and its phycsical Active counterpart has the same coordinator.
        /// </summary>
        /// <returns>True if ClusterHealth 'seems' ok otherwise false, True also in cas of everyother TOPOLOGY other than POR </returns>
        private bool VerifyInitialembers()
        {
            if (initial_mbrs != null && initial_mbrs.Count > 1)
            {
                for (int i = 0; i < initial_mbrs.Count; i++)
                {
                    PingRsp mbr = (PingRsp)initial_mbrs[i];
                    for (int j = i + 1; j <= initial_mbrs.Count - (i + 1); j++)
                    {
                        PingRsp ReplicaMbr = (PingRsp)initial_mbrs[j];
                        if (mbr.OwnAddress.IpAddress.Equals(ReplicaMbr.OwnAddress.IpAddress) && !mbr.CoordAddress.Equals(ReplicaMbr.CoordAddress))
                            return false;
                    }
                }
            }
            return true;
        }

        public override bool isInStateTransfer
        {
            get
            {
                return false;
            }
        }
	}

   
}
