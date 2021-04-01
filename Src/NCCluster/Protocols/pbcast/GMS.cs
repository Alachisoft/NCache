// $Id: GMS.java,v 1.17 2004/09/03 12:28:04 belaban Exp $
using System;
using System.Threading;
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Mirroring;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NGroups.Stack;
using Alachisoft.NGroups.Util;
using System.Collections.Generic;

namespace Alachisoft.NGroups.Protocols.pbcast
{

    /// <summary> Group membership protocol. Handles joins/leaves/crashes (suspicions) and emits new views
    /// accordingly. Use VIEW_ENFORCER on top of this layer to make sure new members don't receive
    /// any messages until they are members.
    /// </summary>
    internal class GMS : Protocol
    {
        override public string Name
        {
            get
            {
                return "GMS";
            }

        }
        virtual public GmsImpl Impl
        {
            get
            {
                return impl;
            }

            set
            {
                lock (impl_mutex)
                {
                    impl = value;
                }
            }

        }
        /// <summary>Sends down a GET_DIGEST event and waits for the GET_DIGEST_OK response, or
        /// timeout, whichever occurs first 
        /// </summary>
        /// <summary>Send down a SET_DIGEST event </summary>
        virtual public Digest Digest
        {
            get
            {
                Digest ret = null;

                lock (digest_mutex)
                {
                    digest = null;
                    passDown(new Event(Event.GET_DIGEST));
                    if (digest == null)
                    {
                        try
                        {
                            System.Threading.Monitor.Wait(digest_mutex, TimeSpan.FromMilliseconds(digest_timeout));
                        }
                        catch (System.Exception ex)
                        {
                            Stack.NCacheLog.Error("GMS.Digest", ex.Message);
                        }
                    }
                    if (digest != null)
                    {
                        ret = digest;
                        digest = null;
                        return ret;
                    }
                    else
                    {
                        Stack.NCacheLog.Error("digest could not be fetched from PBCAST layer");
                        return null;
                    }
                }
            }

            set
            {
                passDown(new Event(Event.SET_DIGEST, value));
            }

        }
        private GmsImpl impl = null;
        public Address local_addr = null;
        public string group_addr = null;
        public string subGroup_addr = null;
        public Membership members = new Membership(); // real membership
        public Membership tmp_members = new Membership(); // base for computing next view

        /// <summary>Members joined but for which no view has been received yet </summary>
        public System.Collections.ArrayList joining = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(7));

        /// <summary>Members excluded from group, but for which no view has been received yet </summary>
        public System.Collections.ArrayList leaving = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(7));

        public ViewId view_id = null;
        public long ltime = 0;
        public long join_timeout = 3000;
        public long join_retry_timeout = 1000;
        public long leave_timeout = 5000;
        public long digest_timeout = 5000; // time to wait for a digest (from PBCAST). should be fast
        public long merge_timeout = 10000; // time to wait for all MERGE_RSPS

        public object impl_mutex = new object(); // synchronizes event entry into impl
        private object digest_mutex = new object(); // synchronizes the GET_DIGEST/GET_DIGEST_OK events
        private Digest digest = null; // holds result of GET_DIGEST event
        private System.Collections.Hashtable impls = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable(3));
        private bool shun = false;
        private bool print_local_addr = true;

        internal bool disable_initial_coord = false; // can the member become a coord on startup or not ?
        internal const string CLIENT = "Client";
        internal const string COORD = "Coordinator";
        internal const string PART = "Participant";
        internal int join_retry_count = 20; //Join retry count with same coordinator;

        private object join_mutex = new object();//Stops simoultaneous node joining

        internal TimeScheduler timer = null;
        private object acquireMap_mutex = new object(); //synchronizes the HASHMAP_REQ/HASHMAP_RESP events
        
        private object _mapMainMutex = new object();
        internal object _hashmap;

        //=======================================
        internal System.Collections.Hashtable _subGroupMbrsMap = new System.Collections.Hashtable();
        internal System.Collections.Hashtable _mbrSubGroupMap = new System.Collections.Hashtable();
        //=======================================

        /// <summary>Max number of old members to keep in history </summary>
        protected internal int num_prev_mbrs = 50;

        /// <summary>Keeps track of old members (up to num_prev_mbrs) </summary>
        internal BoundedList prev_members = null;

        object suspect_verify_mutex = new object();
        NodeStatus nodeStatus;
        Address nodeTobeSuspect;
        internal bool isPartReplica;
        internal ArrayList disconnected_nodes = new ArrayList();
        internal string unique_id;
        internal Hashtable nodeGmsIds = new Hashtable();
        private String _uniqueID = String.Empty;

        private bool _nodeJoiningInProgress = false;
        private bool _isStarting = true;
        ViewPromise _promise;

        //TODO: how much should this be
        internal int _castViewChangeTimeOut = 15000;
        private Address _memberBeingHandled;
        private bool _isLeavingInProgress;


        private object acquirePermission_mutex = new object(); //synchronizes the ASK_JOIN/ASK_JOIN_RESP events

        private object membership_mutex = new object(); //synchronizes the ASK_JOIN/ASK_JOIN_RESP events

        private bool _membershipChangeInProgress = false;

        private object resume_mutex = new object(); //synchronizes the ASK_JOIN/ASK_JOIN_RESP events

        internal bool _allowJoin = false;
        internal bool _doReDiscovery = true;
        internal bool _stateTransferInProcess;
        internal object _syncLock = new object();
        private DateTime _stateTransferMarkTime = DateTime.Now;
        internal SateTransferPromise _stateTransferPromise;
        internal int _stateTransferQueryTimesout = 3000;
        private bool _startedAsMirror;
        internal string versionType;
        internal string environmentName;
        internal bool isExpress;

        public GMS()
        {
            initState();
        }

        /// <summary>
        /// This flag is set to true if we need to intitialize a unique identifier that is 
        /// shared by all the nodes participating in cluster.
        /// </summary>
        public String UniqueID
        {
            get { return _uniqueID; }
            set { _uniqueID = value; }
        }

        public override System.Collections.ArrayList requiredDownServices()
        {
            System.Collections.ArrayList retval = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(3));
            retval.Add((System.Int32)Event.GET_DIGEST);
            retval.Add((System.Int32)Event.SET_DIGEST);
            retval.Add((System.Int32)Event.FIND_INITIAL_MBRS);
            return retval;
        }


        public override void init()
        {
            unique_id = Guid.NewGuid().ToString();
            prev_members = new BoundedList(num_prev_mbrs);
            timer = stack != null ? stack.timer : null;
            if (timer == null)
                throw new System.Exception("GMS.init(): timer is null");
            if (impl != null)
                impl.init();
        }

        public override void start()
        {
            if (impl != null)
                impl.start();

        }

        public override void stop()
        {
            if (impl != null)
                impl.stop();
            if (prev_members != null)
                prev_members.removeAll();
        }


        public virtual void becomeCoordinator()
        {
            CoordGmsImpl tmp = (CoordGmsImpl)impls[COORD];

            if (tmp == null)
            {
                tmp = new CoordGmsImpl(this);
                impls[COORD] = tmp;
            }
            tmp.leaving = false;
            Impl = tmp;
        }

        public bool IsCoordinator
        {
            get
            {
                return Impl is CoordGmsImpl;
            }
        }

        public virtual void becomeParticipant()
        {
            ParticipantGmsImpl tmp = (ParticipantGmsImpl)impls[PART];

            if (tmp == null)
            {
                tmp = new ParticipantGmsImpl(this);
                impls[PART] = tmp;
            }
            tmp.leaving = false;
            Impl = tmp;
        }

        public virtual void becomeClient()
        {
            ClientGmsImpl tmp = (ClientGmsImpl)impls[CLIENT];

            if (tmp == null)
            {
                tmp = new ClientGmsImpl(this);
                impls[CLIENT] = tmp;
            }
            tmp.initial_mbrs.Clear();
            Impl = tmp;
        }


        internal virtual bool haveCoordinatorRole()
        {
            return impl != null && impl is CoordGmsImpl;
        }


        public void MarkStateTransferInProcess()
        {
            if (isPartReplica)
            {
                lock (_syncLock)
                {
                    _stateTransferMarkTime = DateTime.Now;
                    _stateTransferInProcess = true;
                }
            }
        }

        public void MarkStateTransferCompleted()
        {
            if (isPartReplica)
                _stateTransferInProcess = false;
        }

        public bool GetStateTransferStatus()
        {
            if (isPartReplica)
            {
                if (_startedAsMirror) return false;
                lock (_syncLock)
                {
                    if (!_stateTransferInProcess) return false;
                    TimeSpan timeElapsed = DateTime.Now - _stateTransferMarkTime;

                    if (timeElapsed.TotalSeconds >= 20)
                    {
                        _stateTransferInProcess = false;
                        return false;
                    }
                    else
                        return true;
                }
            }

            return false;
        }
        /// <summary> Computes the next view. Returns a copy that has <code>old_mbrs</code> and
        /// <code>suspected_mbrs</code> removed and <code>new_mbrs</code> added.
        /// </summary>
        public virtual View getNextView(System.Collections.ArrayList new_mbrs, System.Collections.ArrayList old_mbrs, System.Collections.ArrayList suspected_mbrs)
        {
            System.Collections.ArrayList mbrs;
            long vid = 0;
            View v;
            Membership tmp_mbrs = null;
            Address tmp_mbr;

            lock (members)
            {

                if (view_id == null)
                {
                    Stack.NCacheLog.Error("pb.GMS.getNextView()", "view_id is null");
                    return null; // this should *never* happen !
                }
                vid = System.Math.Max(view_id.Id, ltime) + 1;
                ltime = vid;

                Stack.NCacheLog.Debug("pb.GMS.getNextView()", "VID=" + vid + ", current members=" + Global.CollectionToString(members.Members) + ", new_mbrs=" + Global.CollectionToString(new_mbrs) + ", old_mbrs=" + Global.CollectionToString(old_mbrs) + ", suspected_mbrs=" + Global.CollectionToString(suspected_mbrs));

                tmp_mbrs = tmp_members.copy(); // always operate on the temporary membership
                tmp_mbrs.remove(suspected_mbrs);
                tmp_mbrs.remove(old_mbrs);
                tmp_mbrs.add(new_mbrs);
                mbrs = tmp_mbrs.Members;
                v = new View(local_addr, vid, mbrs);
                v.CoordinatorGmsId = unique_id;
                // Update membership (see DESIGN for explanation):
                tmp_members.set(mbrs);

                // Update joining list (see DESIGN for explanation)
                if (new_mbrs != null)
                {
                    for (int i = 0; i < new_mbrs.Count; i++)
                    {
                        tmp_mbr = (Address)new_mbrs[i];
                        if (!joining.Contains(tmp_mbr))
                            joining.Add(tmp_mbr);
                    }
                }

                // Update leaving list (see DESIGN for explanations)
                if (old_mbrs != null)
                {
                    for (System.Collections.IEnumerator it = old_mbrs.GetEnumerator(); it.MoveNext(); )
                    {
                        Address addr = (Address)it.Current;
                        if (!leaving.Contains(addr))
                            leaving.Add(addr);
                    }
                }
                if (suspected_mbrs != null)
                {
                    for (System.Collections.IEnumerator it = suspected_mbrs.GetEnumerator(); it.MoveNext(); )
                    {
                        Address addr = (Address)it.Current;
                        if (!leaving.Contains(addr))
                            leaving.Add(addr);
                    }
                }

                Stack.NCacheLog.Debug("pb.GMS.getNextView()", "new view is " + v);
                return v;
            }
        }


        /// <summary>Compute a new view, given the current view, the new members and the suspected/left
        /// members. Then simply mcast the view to all members. This is different to the VS GMS protocol,
        /// in which we run a FLUSH protocol which tries to achive consensus on the set of messages mcast in
        /// the current view before proceeding to install the next view.
        /// The members for the new view are computed as follows:
        /// <pre>
        /// existing          leaving        suspected          joining
        /// 1. new_view      y                 n               n                 y
        /// 2. tmp_view      y                 y               n                 y
        /// (view_dest)
        /// </pre>
        /// <ol>
        /// <li>
        /// The new view to be installed includes the existing members plus the joining ones and
        /// excludes the leaving and suspected members.
        /// <li>
        /// A temporary view is sent down the stack as an <em>event</em>. This allows the bottom layer
        /// (e.g. UDP or TCP) to determine the members to which to send a multicast message. Compared
        /// to the new view, leaving members are <em>included</em> since they have are waiting for a
        /// view in which they are not members any longer before they leave. So, if we did not set a
        /// temporary view, joining members would not receive the view (signalling that they have been
        /// joined successfully). The temporary view is essentially the current view plus the joining
        /// members (old members are still part of the current view).
        /// </ol>
        /// </summary>
        /// <returns> View The new view
        /// </returns>
        public virtual View castViewChange(System.Collections.ArrayList new_mbrs, System.Collections.ArrayList old_mbrs, System.Collections.ArrayList suspected_mbrs, object mapsPackage)
        {
            View new_view;

            new_view = getNextView(new_mbrs, old_mbrs, suspected_mbrs);

            if (new_view == null) 
                return null;

            if (mapsPackage != null)
            {
                object[] distributionAndMirrorMaps = (object[])mapsPackage;
                if (distributionAndMirrorMaps[0] != null)
                {
                    DistributionMaps maps = (DistributionMaps)distributionAndMirrorMaps[0];
                    new_view.DistributionMaps = maps;
                }

                if (distributionAndMirrorMaps[1] != null)
                {
                    new_view.MirrorMapping = (CacheNode[])distributionAndMirrorMaps[1];
                }

            }

            //===============================================
            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("GMS.castViewChange()", "created a new view id = " + new_view.Vid);
            new_view.SequencerTbl = this._subGroupMbrsMap;
            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("GMS.castViewChange()", "new_view.SequencerTbl.count = " + new_view.SequencerTbl.Count);
            new_view.MbrsSubgroupMap = this._mbrSubGroupMap;
            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("GMS.castViewChange()", "new_view.MbrsSubgroupMap.count = " + new_view.MbrsSubgroupMap.Count);
            //===============================================

            castViewChange(new_view);
            return new_view;
        }


        public virtual void castViewChange(View new_view)
        {
            castViewChange(new_view, null);
        }


        public virtual void castViewChange(View new_view, Digest digest)
        {
            Message view_change_msg;
            HDR hdr;

            Stack.NCacheLog.Debug("pb.GMS.castViewChange()", "mcasting view {" + new_view + "} (" + new_view.size() + " mbrs)\n");
            
            view_change_msg = new Message(); // bcast to all members


            hdr = new HDR(HDR.VIEW, new_view);
            hdr.digest = digest;
            view_change_msg.putHeader(HeaderType.GMS, hdr);
            view_change_msg.Dests = new_view.Members.Clone() as ArrayList;

            if (stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("CastView.Watch", "Count of members: " + new_view.Members.Count.ToString());
            _promise = new ViewPromise(new_view.Members.Count);

            bool waitForViewAcknowledgement = true;
            if (!new_view.containsMember(local_addr)) //i am leaving
            {
                waitForViewAcknowledgement = false;
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("GMS.castViewChange()", "I am coordinator and i am leaving");
                passDown(new Event(Event.MSG, view_change_msg, Priority.High));
            }
            else
                passDown(new Event(Event.MSG, view_change_msg, Priority.High));

            if (waitForViewAcknowledgement)
            {
                _promise.WaitResult(_castViewChangeTimeOut);

                if (!_promise.AllResultsReceived()) //retry
                {
                    view_change_msg.Dests = new_view.Members.Clone() as ArrayList;
                    passDown(new Event(Event.MSG, view_change_msg, Priority.High));
                    _promise.WaitResult(_castViewChangeTimeOut);
                }

                if (_promise.AllResultsReceived())
                {
                    Stack.NCacheLog.CriticalInfo("GMS.CastViewChange", "View applied on cluster.");
                }
            }
        }


        /// <summary> Sets the new view and sends a VIEW_CHANGE event up and down the stack. If the view is a MergeView (subclass
        /// of View), then digest will be non-null and has to be set before installing the view.
        /// </summary>
        public virtual void installView(View new_view, Digest digest)
        {
            if (digest != null)
                mergeDigest(digest);
            installView(new_view);
        }

        private void SendViewAcknowledgment(Address coordinator)
        {
            Message m = new Message(coordinator, null, null);
            HDR hdr = new HDR(HDR.VIEW_RESPONSE, true);
            m.putHeader(HeaderType.GMS, hdr);
            passDown(new Event(Event.MSG, m, Priority.High));
        }

        /// <summary> Sets the new view and sends a VIEW_CHANGE event up and down the stack.</summary>
        public virtual void installView(View new_view)
        {
            Stack.NCacheLog.CriticalInfo("GMS.InstallView", "Installing new View " + local_addr.ToString() + " --> " + new_view);

            Address coord = null;
            try
            {
                //Lest inform coordinator about view receiption
                SendViewAcknowledgment(new_view.Coordinator);

                int rc;
                ViewId vid = new_view.Vid;
                System.Collections.ArrayList mbrs = new_view.Members;
                
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("[local_addr=" + local_addr + "] view is " + new_view);

                // Discards view with id lower than our own. Will be installed without check if first view
                if (view_id != null)
                {
                    rc = vid.CompareTo(view_id);
                    if (rc <= 0)
                    {
                        Stack.NCacheLog.Error("[" + local_addr + "] received view <= current view;" + " discarding it (current vid: " + view_id + ", new vid: " + vid + ')');
                        Event viewEvt = new Event(Event.VIEW_CHANGE_OK, null, Priority.High);
                        passDown(viewEvt);
                        return;
                    }

                    Address currentCoodinator = determineCoordinator();
                    Address newCoordinator = new_view.Coordinator;
                    Address sender = vid.CoordAddress; // creater of the view

                    if (!currentCoodinator.Equals(newCoordinator) && !newCoordinator.Equals(local_addr) && !sender.Equals(currentCoodinator))
                    {
                        Stack.NCacheLog.CriticalInfo("GMS.InstallView", "Force Join Cluster");
                        if (!new_view.ForceInstall)
                        {
                            if (!VerifySuspect(currentCoodinator))
                            {
                                if (isPartReplica && newCoordinator.IpAddress.Equals(local_addr.IpAddress))
                                {
                                    Stack.NCacheLog.Error("GMS.installView", " though my coordinator is not down, however view is accepted because it is generated on my main node");
                                }
                                else
                                {
                                    Stack.NCacheLog.Error("GMS.installView", "rejecting the view from " + newCoordinator + " as my own coordinator[" + currentCoodinator + "] is not down");
                                    Event viewEvt = new Event(Event.VIEW_CHANGE_OK, null, Priority.High);
                                    passDown(viewEvt);

                                    //we should inform the coordinator of this view that i can't be the member
                                    //of your view as my own coordinator is alive.

                                    Message msg = new Message(new_view.Coordinator, null, new byte[0]);
                                    msg.putHeader(HeaderType.GMS, new GMS.HDR(GMS.HDR.VIEW_REJECTED, local_addr));
                                    passDown(new Event(Event.MSG, msg, Priority.High));

                                    return;
                                }
                            }
                        }
                    }
                }


                ltime = System.Math.Max(vid.Id, ltime); // compute Lamport logical time

                /* Check for self-inclusion: if I'm not part of the new membership, I just discard it.
                This ensures that messages sent in view V1 are only received by members of V1 */
                if (checkSelfInclusion(mbrs) == false)
                {
                    Stack.NCacheLog.Error("GMS.InstallView", "CheckSelfInclusion() failed, " + local_addr + " is not a member of view " + new_view + "; discarding view");

                    // only shun if this member was previously part of the group. avoids problem where multiple
                    // members (e.g. X,Y,Z) join {A,B} concurrently, X is joined first, and Y and Z get view
                    // {A,B,X}, which would cause Y and Z to be shunned as they are not part of the membership
                    // bela Nov 20 2003
                    if (shun && local_addr != null && prev_members.contains(local_addr))
                    {
                        Stack.NCacheLog.CriticalInfo("I (" + local_addr + ") am being shunned, will leave and " + "rejoin group (prev_members are " + prev_members + ')');
                        passUp(new Event(Event.EXIT));
                    }
                    return;
                }

                lock (members)
                {
                    //@UH Members are same as in the previous view. No need to apply view
                    if (view_id != null)
                    {
                        Membership newMembers = new Membership(mbrs);
                        if (members.Equals(newMembers) && vid.CoordAddress.Equals(view_id.CoordAddress))
                        {

                            Stack.NCacheLog.Error("GMS.InstallView", "[" + local_addr + "] received view has the same members as current view;" + " discarding it (current vid: " + view_id + ", new vid: " + vid + ')');
                            Event viewEvt = new Event(Event.VIEW_CHANGE_OK, null, Priority.High);

                            //3 joining, leaving and tmp_members are needed to be synchronized even if view is same
                            Global.ICollectionSupport.RemoveAll(joining, mbrs); // remove all members in mbrs from joining
                            // remove all elements from 'leaving' that are not in 'mbrs'
                            Global.ICollectionSupport.RetainAll(leaving, mbrs);

                            tmp_members.add(joining); // add members that haven't yet shown up in the membership
                            tmp_members.remove(leaving); // remove members that haven't yet been removed from the membership

                            passDown(viewEvt);
                            return;
                        }
                    }

                    //=========================================
                    //
                    Stack.NCacheLog.CriticalInfo("GMS.InstallView", "Installing view in GMS Layer.");

                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("GMS.InstallView " + new_view.ToString() + "\\n" + "seq tble : " + new_view.SequencerTbl.Count);
                    this._subGroupMbrsMap = new_view.SequencerTbl.Clone() as System.Collections.Hashtable;
                    this._mbrSubGroupMap = new_view.MbrsSubgroupMap.Clone() as System.Collections.Hashtable;
                    //=========================================

                    // serialize access to views
                    // assign new_view to view_id
                    view_id = vid.Copy();
                    Stack.NCacheLog.CriticalInfo("GMS.InstallView", "View ID = " + view_id.ToString());

                    // Set the membership. Take into account joining members
                    if (mbrs != null && mbrs.Count > 0)
                    {
                        for (int i = 0; i < members.size(); i++)
                        {
                            Address mbr = members.elementAt(i);
                            if (!mbrs.Contains(mbr))
                                RemoveGmsId(mbr);
                        }
                        Hashtable gmsIds = new_view.GmsIds;

                        if (gmsIds != null)
                        {
                            IDictionaryEnumerator ide = gmsIds.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("GMS.InstallView", "mbr  = " + ide.Key + " ; gms_id = " + ide.Value);
                                AddGmsId((Address)ide.Key, (string)ide.Value);
                            }
                        }
                        for (int i = 0; i < mbrs.Count; i++)
                        {
                            Stack.NCacheLog.CriticalInfo("GMS.InstallView", "Members.set = " + mbrs[i] != null ? mbrs[i].ToString() : null);
                        }

                        members.set(mbrs);
                        tmp_members.set(members);
                        Global.ICollectionSupport.RemoveAll(joining, mbrs); // remove all members in mbrs from joining
                        // remove all elements from 'leaving' that are not in 'mbrs'
                        Global.ICollectionSupport.RetainAll(leaving, mbrs);

                        tmp_members.add(joining); // add members that haven't yet shown up in the membership
                        tmp_members.remove(leaving); // remove members that haven't yet been removed from the membership

                        // add to prev_members
                        for (System.Collections.IEnumerator it = mbrs.GetEnumerator(); it.MoveNext(); )
                        {
                            Address addr = (Address)it.Current;
                            if (!prev_members.contains(addr))
                                prev_members.add(addr);
                        }
                    }

                    // Send VIEW_CHANGE event up and down the stack:
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("GMS.installView", "broadcasting view change within stack");


                    coord = determineCoordinator();
                    // changed on suggestion by yaronr and Nicolas Piedeloupe
                    if (coord != null && coord.Equals(local_addr) && !haveCoordinatorRole())
                    {
                        becomeCoordinator();
                    }
                    else
                    {
                        if (haveCoordinatorRole() && !local_addr.Equals(coord))
                            becomeParticipant();
                    }
                  
                    MarkStateTransferInProcess();
                    Event view_event = new Event(Event.VIEW_CHANGE, new_view.Clone(), Priority.High);
                    passDown(view_event); // needed e.g. by failure detector or UDP
                }
            }
            finally
            {
            }
        }


        protected internal virtual Address determineCoordinator()
        {
            lock (members)

            {
                return members != null && members.size() > 0 ? (Address)members.elementAt(0) : null;
            }
        }

        public void AddGmsId(Address node, string id)
        {
            if (node != null)
                nodeGmsIds[node] = id;
        }

        public string GetNodeGMSId(Address node)
        {
            if (node != null)
                return nodeGmsIds[node] as string;

            return null;
        }

        public void RemoveGmsId(Address node)
        {
            if (node != null)
                nodeGmsIds.Remove(node);
        }

        public void RemoveGmsId(ArrayList nodes)
        {
            foreach (Address node in nodes)
            {
                if (node != null)
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
        /// <summary>Checks whether the potential_new_coord would be the new coordinator (2nd in line) </summary>
        protected internal virtual bool wouldBeNewCoordinator(Address potential_new_coord)
        {
            Address new_coord = null;

            if (potential_new_coord == null)
                return false;

            lock (members)
            {
                if (members.size() < 2)
                    return false;
                new_coord = (Address)members.elementAt(1); // member at 2nd place
                if (new_coord != null && new_coord.Equals(potential_new_coord))
                    return true;
                return false;
            }
        }


        /// <summary>Returns true if local_addr is member of mbrs, else false </summary>
        protected internal virtual bool checkSelfInclusion(System.Collections.ArrayList mbrs)
        {
            object mbr;
            if (mbrs == null)
                return false;
            for (int i = 0; i < mbrs.Count; i++)
            {
                mbr = mbrs[i];
                if (mbr != null && local_addr.Equals(mbr))
                    return true;
            }
            return false;
        }


        public virtual View makeView(System.Collections.ArrayList mbrs)
        {
            Address coord = null;
            long id = 0;

            if (view_id != null)
            {
                coord = view_id.CoordAddress;
                id = view_id.Id;
            }
            View view = new View(coord, id, mbrs);
            view.CoordinatorGmsId = unique_id;
            return view;
        }


        public virtual View makeView(System.Collections.ArrayList mbrs, ViewId vid)
        {
            Address coord = null;
            long id = 0;

            if (vid != null)
            {
                coord = vid.CoordAddress;
                id = vid.Id;
            }
            View view = new View(coord, id, mbrs);
            view.CoordinatorGmsId = unique_id;
            return view;
        }


        /// <summary>Send down a MERGE_DIGEST event </summary>
        public virtual void mergeDigest(Digest d)
        {
            passDown(new Event(Event.MERGE_DIGEST, d));
        }


        public override void up(Event evt)
        {
            object obj;
            Message msg;
            HDR hdr;
            MergeData merge_data;

            switch (evt.Type)
            {


                case Event.MSG:
                    msg = (Message)evt.Arg;

                    obj = msg.getHeader(HeaderType.GMS);
                    if (obj == null || !(obj is HDR))
                        break;
                    hdr = (HDR)msg.removeHeader(HeaderType.GMS);
                    switch (hdr.type)
                    {

                        case HDR.JOIN_REQ:
                            object[] args = new object[4];
                            args[0] = hdr.mbr;
                            args[1] = hdr.subGroup_name;
                            args[2] = hdr.isStartedAsMirror;
                            args[3] = hdr.GMSId;
                            ThreadPool.QueueUserWorkItem(new WaitCallback(handleJoinrequestAsync), args);
                            //handleJoinRequest(hdr.mbr, hdr.subGroup_name, hdr.isStartedAsMirror, hdr.GMSId);
                            break;

                        case HDR.SPECIAL_JOIN_REQUEST:
                            HandleSpecialJoinRequest(hdr.mbr, hdr.GMSId);
                            break;

                        case HDR.JOIN_RSP:
                            MarkStateTransferInProcess();
                            impl.handleJoinResponse(hdr.join_rsp);
                            break;

                        
                        case HDR.LEAVE_REQ:
                            Stack.NCacheLog.Debug("received LEAVE_REQ " + hdr + " from " + msg.Src);

                            if (hdr.mbr == null)
                            {
                                Stack.NCacheLog.Error("LEAVE_REQ's mbr field is null");
                                return;
                            }
                            if (isPartReplica && IsCoordinator)
                            {
                                //if replica node on the coordinator is leaving then send a special event to TCP
                                //to mark himself leaving. This way other node asking for death status through keep
                                //alive will get dead status.
                                if (hdr.mbr != null && hdr.mbr.IpAddress.Equals(local_addr.IpAddress))
                                {
                                    down(new Event(Event.I_AM_LEAVING));
                                }
                            }
                            ThreadPool.QueueUserWorkItem(new WaitCallback(handleLeaveAsync), new object[] { hdr.mbr, false });

                            break;

                        case HDR.LEAVE_RSP:
                            impl.handleLeaveResponse();
                            break;

                        case HDR.VIEW_RESPONSE:
                            if (_promise != null)
                                _promise.SetResult(hdr.arg);
                            break;

                        case HDR.VIEW:
                            if (hdr.view == null)
                            {
                                Stack.NCacheLog.Error("[VIEW]: view == null");
                                return;
                            }
                            else
                                Stack.NCacheLog.CriticalInfo("GMS.Up", "received view from :" + msg.Src + " ; view = " + hdr.view);
                            impl.handleViewChange(hdr.view, hdr.digest);
                            break;


                        case HDR.MERGE_REQ:
                            impl.handleMergeRequest(msg.Src, hdr.merge_id);
                            break;


                        case HDR.MERGE_RSP:
                            merge_data = new MergeData(msg.Src, hdr.view, hdr.digest);
                            merge_data.merge_rejected = hdr.merge_rejected;
                            impl.handleMergeResponse(merge_data, hdr.merge_id);
                            break;


                        case HDR.INSTALL_MERGE_VIEW:
                            impl.handleMergeView(new MergeData(msg.Src, hdr.view, hdr.digest), hdr.merge_id);
                            break;


                        case HDR.CANCEL_MERGE:
                            impl.handleMergeCancelled(hdr.merge_id);
                            break;

                        case HDR.CAN_NOT_CONNECT_TO:
                            impl.handleCanNotConnectTo(msg.Src, hdr.nodeList);
                            break;

                        case HDR.LEAVE_CLUSTER:
                            string gmsId = hdr.arg as string;//reported gms id
                            string myGmsId = GetNodeGMSId(local_addr);

                            if (gmsId != null && myGmsId != null && gmsId.Equals(myGmsId))
                            {
                                ThreadPool.QueueUserWorkItem(new WaitCallback(handleLeaveClusterRequestAsync), hdr.mbr);
                            }
                            break;

                        case HDR.CONNECTION_BROKEN:
                            impl.handleConnectionBroken(msg.Src, hdr.mbr);
                            break;

                        case HDR.VIEW_REJECTED:
                            impl.handleViewRejected(hdr.mbr);
                            break;

                        case HDR.INFORM_NODE_REJOINING:
                            impl.handleInformNodeRejoining(msg.Src, hdr.mbr);
                            break;

                        case HDR.RESET_ON_NODE_REJOINING:
                            impl.handleResetOnNodeRejoining(msg.Src, hdr.mbr, hdr.view);
                            break;

                        case HDR.RE_CHECK_CLUSTER_HEALTH:
                            Thread t = new Thread(new ParameterizedThreadStart(impl.ReCheckClusterHealth));
                            t.Start(hdr.mbr);
                            break;

                        case HDR.INFORM_ABOUT_NODE_DEATH:
                            //Replica is not supposed to handle this event
                            if (isPartReplica && _startedAsMirror) break;

                            impl.handleInformAboutNodeDeath(msg.Src, (Address)hdr.arg);
                            break;

                        case HDR.IS_NODE_IN_STATE_TRANSFER:
                            impl.handleIsClusterInStateTransfer(msg.Src);
                            break;

                        case HDR.IS_NODE_IN_STATE_TRANSFER_RSP:
                            if (_stateTransferPromise != null)
                            {
                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("gms.UP", "(state transfer rsp) sender: " + msg.Src + " ->" + hdr.arg);
                                _stateTransferPromise.SetResult(hdr.arg);
                            }
                            break;

                        default:
                            Stack.NCacheLog.Error("HDR with type=" + hdr.type + " not known");
                            break;

                    }

                    return; // don't pass up


                case Event.CONNECT_OK:
                // sent by someone else, but WE are responsible for sending this !
                case Event.DISCONNECT_OK:  // dito (e.g. sent by UDP layer). Don't send up the stack
                    return;

                case Event.GET_NODE_STATUS_OK:
                    lock (suspect_verify_mutex)
                    {
                        NodeStatus status = evt.Arg as NodeStatus;
                        if (status.Node != null && status.Node.Equals(nodeTobeSuspect))
                        {
                            nodeStatus = status;
                            Monitor.PulseAll(suspect_verify_mutex);
                        }
                    }
                    break;

                case Event.SET_LOCAL_ADDRESS:
                    local_addr = (Address)evt.Arg;
                    break; // pass up


                case Event.SUSPECT:
                    ThreadPool.QueueUserWorkItem(new WaitCallback(handleSuspectAsync), evt.Arg);
                    break; // pass up


                case Event.UNSUSPECT:
                    impl.unsuspect((Address)evt.Arg);
                    return; // discard


                case Event.MERGE:
                    impl.merge((System.Collections.ArrayList)evt.Arg);
                    return; // don't pass up

                case Event.CONNECTION_FAILURE:
                    impl.handleConnectionFailure(evt.Arg as ArrayList);
                    return;//dont passup

                case Event.NODE_REJOINING:
                    impl.handleNodeRejoining(evt.Arg as Address);
                    return;

                case Event.CONNECTION_BREAKAGE:
                    Address node = evt.Arg as Address;
                    if (!disconnected_nodes.Contains(node))
                        disconnected_nodes.Add(node);
                    break;

                case Event.CONNECTION_RE_ESTABLISHED:
                    node = evt.Arg as Address;
                    if (disconnected_nodes.Contains(node))
                        disconnected_nodes.Remove(node);
                    break;
            }

            if (impl.handleUpEvent(evt))
                passUp(evt);
        }



        /// <summary>This method is overridden to avoid hanging on getDigest(): when a JOIN is received, the coordinator needs
        /// to retrieve the digest from the PBCAST layer. It therefore sends down a GET_DIGEST event, to which the PBCAST layer
        /// responds with a GET_DIGEST_OK event.<p>
        /// However, the GET_DIGEST_OK event will not be processed because the thread handling the JOIN request won't process
        /// the GET_DIGEST_OK event until the JOIN event returns. The receiveUpEvent() method is executed by the up-handler
        /// thread of the lower protocol and therefore can handle the event. All we do here is unblock the mutex on which
        /// JOIN is waiting, allowing JOIN to return with a valid digest. The GET_DIGEST_OK event is then discarded, because
        /// it won't be processed twice.
        /// </summary>
        public override void receiveUpEvent(Event evt)
        {
            if (evt.Type == Event.GET_DIGEST_OK)
            {
                lock (digest_mutex)
                {
                    digest = (Digest)evt.Arg;
                    System.Threading.Monitor.PulseAll(digest_mutex);
                }
                return;
            }
            base.receiveUpEvent(evt);
        }

        public bool VerifySuspect(Address suspect)
        {
            return VerifySuspect(suspect, true);
        }

        /// <summary>
        /// Verifes whether the given node is dead or not.
        /// </summary>
        /// <param name="suspect">suspected node</param>
        /// <returns>true, if node is dead otherwise false</returns>
        public bool VerifySuspect(Address suspect, bool matchGmsId)
        {
            bool isDead = true;
            string gmsId = null;

            if (suspect != null)
            {
                Stack.NCacheLog.CriticalInfo("GMS.VerifySuspect", " verifying the death of node " + suspect);
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("GMS.VerifySuspect", " verifying the death of node " + suspect);

                gmsId = GmsIds[suspect] as string;
                lock (suspect_verify_mutex)
                {
                    nodeStatus = null;
                    nodeTobeSuspect = suspect;
                    passDown(new Event(Event.GET_NODE_STATUS, suspect, Priority.High));
                    //we wait for the verification

                    Monitor.Wait(suspect_verify_mutex);
                    if (nodeStatus != null)
                    {
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("GMS.VerifySuspect", " node status is " + nodeStatus.ToString());
                        switch (nodeStatus.Status)
                        {
                            case NodeStatus.IS_ALIVE: isDead = false; break;
                            case NodeStatus.IS_DEAD: isDead = true; break;
                            case NodeStatus.IS_LEAVING: isDead = true; break;

                        }
                    }
                }

            }

            if (isDead && matchGmsId)
            {
                //we verify whether current gms id is same as when node was reported suspect.
                string currentGmsId = GmsIds[suspect] as string;

               if (currentGmsId != null && gmsId != null && currentGmsId.Equals(gmsId))
                    return true;
                
                else
                {
                    if (Stack.NCacheLog.IsErrorEnabled) Stack.NCacheLog.CriticalInfo("GMS.VerifySuspect", "node gms ids differ; old : " + gmsId + " new: " + currentGmsId + nodeStatus.ToString());
                    return false;
                }
            }
           

            return isDead;
        }

        /// <summary>
        /// We inform other nodes about the possible death of coordinator
        /// 
        /// </summary>
        /// <param name="otherNodes"></param>
        public void InformOthersAboutCoordinatorDeath(ArrayList otherNodes, Address deadNode)
        {
            if (otherNodes != null && otherNodes.Count > 0)
            {
                Message msg = new Message(null, null, new byte[0]);
                msg.Dests = otherNodes;
                GMS.HDR hdr = new HDR(GMS.HDR.INFORM_ABOUT_NODE_DEATH);
                hdr.arg = deadNode;
                msg.putHeader(HeaderType.GMS, hdr);
                down(new Event(Event.MSG, msg, Priority.High));
            }
        }


        /// <summary>
        /// Checks if state transfer is in progress any where in the cluster. First status is checked
        /// on local node. If current node is not in state transfer then it is verified from other nodes
        /// in the cluster.
        /// </summary>
        /// <returns></returns>
        public bool IsClusterInStateTransfer()
        {
            if (GetStateTransferStatus()) return true;
            //check with other members
            if (this.members != null)
            {
                ArrayList allmembers = this.members.Members;
                if (allmembers != null && allmembers.Count > 0)
                {
                    _stateTransferPromise = new SateTransferPromise(allmembers.Count);
                    Message msg = new Message(null, null, new byte[0]);
                    msg.Dests = allmembers;
                    GMS.HDR hdr = new HDR(GMS.HDR.IS_NODE_IN_STATE_TRANSFER);
                    msg.putHeader(HeaderType.GMS, hdr);
                    down(new Event(Event.MSG, msg, Priority.High));
                    Object objectState = _stateTransferPromise.WaitResult(_stateTransferQueryTimesout);
                    //huma: Service crash fix.
                    bool isInstateTransfer = objectState != null ? (bool)objectState : false;
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("gms.IsClusterInStateTransfer", "result : " + (isInstateTransfer));
                    return isInstateTransfer;
                }
            }
            return false;
        }


        public override void down(Event evt)
        {
            switch (evt.Type)
            {

                case Event.NOTIFY_LEAVING:
                    impl.handleNotifyLeaving();
                    return;
                case Event.CONNECT_PHASE_2:
                    bool isStartedAsMirror = (bool)evt.Arg;
                    _startedAsMirror = isStartedAsMirror;
                    impl.join(local_addr, isStartedAsMirror);
                    passUp(new Event(Event.CONNECT_OK_PHASE_2));
                    break;

                case Event.MARK_CLUSTER_IN_STATETRANSFER:
                    MarkStateTransferInProcess();
                    break;

                case Event.MARK_CLUSTER_STATETRANSFER_COMPLETED:
                    MarkStateTransferCompleted();
                    break;

                case Event.CONNECT:
                    passDown(evt);
                    isStartedAsMirror = false;
                    bool twoPhaseConnect = false;
                    try
                    {
                        object[] addrs = (object[])evt.Arg;
                        group_addr = (string)addrs[0];
                        subGroup_addr = (string)addrs[1];
                        //group_addr = (string)evt.Arg;
                        isStartedAsMirror = (bool)addrs[2];
                        twoPhaseConnect = (bool)addrs[3];
                    }
                    catch (System.InvalidCastException e)
                    {
                        Stack.NCacheLog.Error("[CONNECT]: group address must be a string (channel name)", e.Message);
                    }
                    if (local_addr == null)
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("[CONNECT] local_addr is null");
                    if (!twoPhaseConnect)
                        impl.join(local_addr, isStartedAsMirror);

                    passUp(new Event(Event.CONNECT_OK));
                    return; // don't pass down: was already passed down

             
                case Event.IS_CLUSTER_IN_STATE_TRANSFER:

                    bool isClusterUnderStateTransfer = IsClusterInStateTransfer();

                    passUp(new Event(Event.IS_CLUSTER_IN_STATE_TRANSFER_RSP, isClusterUnderStateTransfer));
                    break;

                    passUp(new Event(Event.MARKED_FOR_MAINTENANCE));
                    break;

                case Event.DISCONNECT:
                    impl.leave((Address)evt.Arg);
                    Thread.Sleep(1000);
                    Stack.NCacheLog.Info("GMS.down()", "passing up DISCONNECT_OK ");

                    passUp(new Event(Event.DISCONNECT_OK));
                    initState(); // in case connect() is called again
                    break; // pass down

                case Event.HASHMAP_RESP:
                    lock (acquireMap_mutex)
                    {
                        _hashmap = evt.Arg;
                        Stack.NCacheLog.Debug("pbcast.GMS.down()", " DistributionMap and MirrorMap response received.");
                        Stack.NCacheLog.Debug("pbcast.GMS.down()", _hashmap == null ? "null map..." : "maps package received.");
                        //pulse the thread waiting to send join response.
                        System.Threading.Monitor.Pulse(acquireMap_mutex);
                    }
                    break;

                case Event.CONFIRM_CLUSTER_STARTUP:
                    object[] arg = (object[])evt.Arg;
                    bool isPOR = (bool)arg[0];
                    int retryNumber = (int)arg[1];

                    if (!_doReDiscovery) return;
                    //If the cluster is singleton perform network cluster checkup
                    if (isPOR)
                    {
                        if (members.Members.Count > 2)
                        {
                            return;
                        }
                    }
                    //This will always be true but to be sure

                    return;
                case Event.HAS_STARTED:
                    lock (join_mutex)
                    {
                        _isStarting = false;
                    }
                    break;
                case Event.ASK_JOIN_RESPONSE:
                    lock (acquirePermission_mutex)
                    {
                        _allowJoin = Convert.ToBoolean(evt.Arg);
                        //pulse the thread waiting to send join response.
                        System.Threading.Monitor.Pulse(acquirePermission_mutex);
                    }
                    break;

            }

            if (impl.handleDownEvent(evt))
                passDown(evt);
        }
        /// <summary>
        /// Generates the next view Id.
        /// </summary>
        /// <returns></returns>
        internal ViewId GetNextViewId()
        {
            long vid = -1;
            lock (members)
            {
                if (view_id == null)
                {
                    Stack.NCacheLog.Error("pb.GMS.getNextView()", "view_id is null");
                    return null; // this should *never* happen !
                }
                vid = System.Math.Max(view_id.Id, ltime) + 1;
                ltime = vid;
            }
            return new ViewId(local_addr, vid);
        }
        /// <summary>Setup the Protocol instance according to the configuration string </summary>
        public override bool setProperties(System.Collections.Hashtable props)
        {
            base.setProperties(props);

            if (stack.StackType == ProtocolStackType.TCP)
            {
                this.up_thread = false;
                this.down_thread = false;
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info(Name + ".setProperties", "part of TCP stack");
            }
            if (props.Contains("shun"))
            {
                shun = Convert.ToBoolean(props["shun"]);
                props.Remove("shun");
            }
          
            //if (props.Contains("env_name"))
            //{
            //    environmentName = Convert.ToString(props["env_name"]);                
            //    props.Remove("env_name");
            //}
            
            if (props.Contains("is_part_replica"))
            {
                isPartReplica = Convert.ToBoolean(props["is_part_replica"]);
                props.Remove("is_part_replica");
            }

            if (props.Contains("print_local_addr"))
            {
                print_local_addr = Convert.ToBoolean(props["print_local_addr"]);
                props.Remove("print_local_addr");
            }

            if (props.Contains("join_timeout"))
            {
                join_timeout = Convert.ToInt64(props["join_timeout"]);
                props.Remove("join_timeout");
            }

            if (props.Contains("join_retry_count"))
            {
                join_retry_count = Convert.ToInt32(props["join_retry_count"]);
                props.Remove("join_retry_count");
            }

            if (props.Contains("join_retry_timeout"))
            {
                join_retry_timeout = Convert.ToInt64(props["join_retry_timeout"]) * 1000;
                props.Remove("join_retry_timeout");
            }

            if (props.Contains("leave_timeout"))
            {
                leave_timeout = Convert.ToInt64(props["leave_timeout"]);
                props.Remove("leave_timeout");
            }

            if (props.Contains("merge_timeout"))
            {
                merge_timeout = Convert.ToInt64(props["merge_timeout"]);
                props.Remove("merge_timeout");
            }

            if (props.Contains("digest_timeout"))
            {
                digest_timeout = Convert.ToInt64(props["digest_timeout"]);
                props.Remove("digest_timeout");
            }

            if (props.Contains("disable_initial_coord"))
            {
                disable_initial_coord = Convert.ToBoolean(props["disable_initial_coord"]);
                props.Remove("disable_initial_coord");
            }

            if (props.Contains("num_prev_mbrs"))
            {
                num_prev_mbrs = Convert.ToInt32(props["num_prev_mbrs"]);
                props.Remove("num_prev_mbrs");
            }

            if (props.Count > 0)
            {
                Stack.NCacheLog.Error("GMS.setProperties(): the following properties are not recognized: \n" + Global.CollectionToString(props.Keys));
                return true;
            }
            return true;
        }

        public bool CheckAllNodesConnected(ArrayList memebers)
        {
            return false;
        }

        /* ------------------------------- Private Methods --------------------------------- */
        private void sendUp(object data)
        {
            Event evt = data as Event;
            lock (evt)
            {
                Monitor.PulseAll(evt);
            }

            up(evt);
        }

        internal virtual void initState()
        {
            becomeClient();
            view_id = null;
        }


        internal bool allowJoin(Address mbr, bool isStartedAsMirror)
        {
           
            //Stack.NCacheLog.DevTrace("gms.allowJoin", "start");

            if (!isPartReplica) return true;
            //new code for disabling the join while in state transfer.
            lock (acquirePermission_mutex)
            {
                _allowJoin = false;


                ArrayList existingMembers = members.Members;
                Address lastJoiney = null;
                if (existingMembers.Count > 0)
                {
                    lastJoiney = existingMembers[existingMembers.Count - 1] as Address;
                    if (isStartedAsMirror)
                    {
                        if (!lastJoiney.IpAddress.Equals(mbr.IpAddress))
                            return false;
                    }
                    else
                    {
                        if (existingMembers.Count > 1)
                        {
                            Address secondLastJoinee = existingMembers[existingMembers.Count - 2] as Address;
                            if (!lastJoiney.IpAddress.Equals(secondLastJoinee.IpAddress))
                                return false;
                        }
                        else
                            return false;
                    }
                }

                if (isStartedAsMirror)
                {
                    if (members.ContainsIP(mbr))
                    {
                        _allowJoin = true;
                    }
                }
                else
                {
                    Stack.NCacheLog.CriticalInfo("GMS.AllowJoin", "Join permission for " + mbr.ToString());
                    bool inStateTransfer = IsClusterInStateTransfer();
                    _allowJoin = !inStateTransfer;
                }
                return _allowJoin;
            }

        }

        internal virtual void HandleSpecialJoinRequest(Address mbr, string gmsId)
        {
            Stack.NCacheLog.CriticalInfo("pbcast.GMS.HandleSpecialJoinRequest()", "mbr=" + mbr);

            if (haveCoordinatorRole())
            {
                return;
            }
            Address new_coord = null;
            System.Collections.ArrayList mbrs = members.Members; // getMembers() returns a *copy* of the membership vector            

            Address sameNode = null;
            if (isPartReplica)
            {
                Membership mbrShip = members.copy();

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

            mbrs.Remove(mbr);
            mbrs.Remove(sameNode);

            for (int i = 0; i < mbrs.Count - 1; i++)
            {
                new_coord = (Address)mbrs[i];
                if (local_addr.Equals(new_coord))
                {
                    becomeCoordinator();
                    impl.handleSuspect(mbr);
                    return;
                }
                else if (!VerifySuspect(new_coord))
                {
                    return;
                }

            }

            Stack.NCacheLog.CriticalInfo("HandleSpecialJoinRequest", "Anomoly at choosing next coordinator");
            return;

        }

        internal void handleLeaveAsync(object args)
        {
            object[] arr = (object[])args;
            
            impl.handleLeave(arr[0] as Address, (bool)arr[1]);
        }

        internal void handleJoinrequestAsync(object args)
        {
            object[] arr = (object[])args;
            handleJoinRequest(arr[0] as Address, arr[1] as string, (bool)arr[2], arr[3] as string);
        }

        internal void handleSuspectAsync(object mbr)
        {
            impl.handleSuspect(mbr as Address);
        }

        internal void handleLeaveClusterRequestAsync(object mbr)
        {
            impl.handleLeaveClusterRequest(mbr as Address);
        }

        internal virtual void handleJoinRequest(Address mbr, string subGroup_name, bool isStartedAsMirror, string gmsId)
        {
            JoinRsp join_rsp = null;
            Message m;
            HDR hdr;

            if (mbr == null)
            {
                Stack.NCacheLog.Error("mbr is null");
                return;
            }

            Stack.NCacheLog.Debug("pbcast.GMS.handleJoinRequest()", "mbr=" + mbr);

            if (!isStartedAsMirror)
            {
                lock (join_mutex)
                {
                    if (_nodeJoiningInProgress || _isLeavingInProgress || (_isStarting && !local_addr.IpAddress.Equals(mbr.IpAddress)))
                    {
                        Stack.NCacheLog.CriticalInfo("GMS.HandleJoinRequest", "node :" + mbr + "joining is in progress.");
                        join_rsp = new JoinRsp(null, null);
                        join_rsp.JoinResult = JoinResult.MembershipChangeAlreadyInProgress;
                       
                        m = new Message(mbr, null, null);
                        hdr = new HDR(HDR.JOIN_RSP, join_rsp);
                        m.putHeader(HeaderType.GMS, hdr);
                        passDown(new Event(Event.MSG, m, Priority.High));
                        return;
                    }
                    else
                    {
                        _nodeJoiningInProgress = true;
                    }
                }
            }

            // 1. Get the new view and digest
            if (members.contains(mbr))
            {

                string oldGmsId = GetGmsId(mbr);

                if (oldGmsId != null && oldGmsId != gmsId)
                {
                    Stack.NCacheLog.Error("pbcast.GMS.handleJoinRequest()", mbr + " has sent a joining request while it is already in member list and has wrong gmsID");
                    join_rsp = null;
                    m = new Message(mbr, null, null);
                    hdr = new HDR(HDR.JOIN_RSP, join_rsp);
                    m.putHeader(HeaderType.GMS, hdr);
                    passDown(new Event(Event.MSG, m, Priority.High));


                    impl.handleSuspect(mbr);

                    lock (join_mutex)
                    {
                        _nodeJoiningInProgress = false;
                    }
                    return;
                }
                else
                {

                    Stack.NCacheLog.Error("pbcast.GMS.handleJoinRequest()", mbr + " has sent a joining request while it is already in member list - Resending current view and digest");
                    View view = new View(this.view_id, members.Members);
                    view.CoordinatorGmsId = unique_id;
                    join_rsp = new JoinRsp(view, this.digest, JoinResult.Success);
                    m = new Message(mbr, null, null);
                    hdr = new HDR(HDR.JOIN_RSP, join_rsp);
                    m.putHeader(HeaderType.GMS, hdr);
                    passDown(new Event(Event.MSG, m, Priority.High));
                    lock (join_mutex)
                    {
                        _nodeJoiningInProgress = false;
                    }
                    return;
                }
            }


            if (allowJoin(mbr, isStartedAsMirror))
            {
                Stack.NCacheLog.Debug("pbcast.GMS.handleJoinRequest()", " joining allowed");
             

                join_rsp = impl.handleJoin(mbr, subGroup_name, isStartedAsMirror, gmsId);

                if (join_rsp == null)
                    Stack.NCacheLog.Error("pbcast.GMS.handleJoinRequest()", impl.GetType().ToString() + ".handleJoin(" + mbr + ") returned null: will not be able to multicast new view");

              
                //sends a request to the caching layer for the new hashmap after this member joins.
                System.Collections.ArrayList mbrs = new System.Collections.ArrayList(1);
                mbrs.Add(mbr);
                
               

                // 2. Send down a local TMP_VIEW event. This is needed by certain layers (e.g. NAKACK) to compute correct digest
                //    in case client's next request (e.g. getState()) reaches us *before* our own view change multicast.
                // Check NAKACK's TMP_VIEW handling for details
                if (join_rsp != null && join_rsp.View != null)
                {
                   
                    //add the hash map as part of view.
                    if (_hashmap != null)
                    {
                        Object[] mapsArray = (Object[])_hashmap;
                        DistributionMaps maps = (DistributionMaps)mapsArray[0];
                        if (maps != null)
                        {
                            join_rsp.View.DistributionMaps = maps;
                        }

                        join_rsp.View.MirrorMapping = mapsArray[1] as CacheNode[];

                    }
                    passDown(new Event(Event.TMP_VIEW, join_rsp.View));
                }
            }
            else
                Stack.NCacheLog.Debug("pbcast.GMS.handleJoinRequest()", " joining not allowed");

            // 3. Return result to client
            m = new Message(mbr, null, null);
            hdr = new HDR(HDR.JOIN_RSP, join_rsp);
            m.putHeader(HeaderType.GMS, hdr);
            passDown(new Event(Event.MSG, m, Priority.High));

            // 4. Bcast the new view
            if (join_rsp != null)
                castViewChange(join_rsp.View);

            lock (join_mutex)
            {
                _nodeJoiningInProgress = false;
            }

        }

        public void handleResetOnNodeRejoining(Address sender, Address node, View view)
        {
            ViewId vid = view.Vid;
            int rc;
            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("GMS.handleResetOnNodeRejoining", "Sequence reset request");
            if (view_id != null)
            {
                rc = vid.CompareTo(view_id);
                if (rc <= 0)
                {
                    return;
                }
                ltime = System.Math.Max(vid.Id, ltime); // compute Lamport logical time
                lock (members)
                {
                    view_id = vid.Copy();
                }
            }
            Event evt = new Event(Event.RESET_SEQUENCE, vid);
            passUp(evt);
        }

        internal class HDR : Header, ICompactSerializable
        {
            public const byte JOIN_REQ = 1;
            public const byte JOIN_RSP = 2;
            public const byte LEAVE_REQ = 3;
            public const byte LEAVE_RSP = 4;
            public const byte VIEW = 5;
            public const byte MERGE_REQ = 6;
            public const byte MERGE_RSP = 7;
            public const byte INSTALL_MERGE_VIEW = 8;
            public const byte CANCEL_MERGE = 9;
            public const byte CAN_NOT_CONNECT_TO = 10;
            public const byte LEAVE_CLUSTER = 11;
            public const byte CONNECTION_BROKEN = 12;
            public const byte CONNECTED_NODES_REQUEST = 13;
            public const byte CONNECTED_NODES_RESPONSE = 14;
            public const byte VIEW_REJECTED = 15;
            public const byte INFORM_NODE_REJOINING = 16;
            public const byte RESET_ON_NODE_REJOINING = 17;
            public const byte RE_CHECK_CLUSTER_HEALTH = 18;
            public const byte VIEW_RESPONSE = 19;
            public const byte SPECIAL_JOIN_REQUEST = 20;
            public const byte INFORM_ABOUT_NODE_DEATH = 21;
            public const byte IS_NODE_IN_STATE_TRANSFER = 22;
            public const byte IS_NODE_IN_STATE_TRANSFER_RSP = 23;
            public const byte MARK_FOR_MAINTENANCE = 24;
            public const byte UNMARK_FOR_MAINTENANCE = 25;


            internal byte type = 0;
            internal View view = null; // used when type=VIEW or MERGE_RSP or INSTALL_MERGE_VIEW
            internal Address mbr = null; // used when type=JOIN_REQ or LEAVE_REQ
            internal JoinRsp join_rsp = null; // used when type=JOIN_RSP
            internal Digest digest = null; // used when type=MERGE_RSP or INSTALL_MERGE_VIEW
            internal object merge_id = null; // used when type=MERGE_REQ or MERGE_RSP or INSTALL_MERGE_VIEW or CANCEL_MERGE
            internal bool merge_rejected = false; // used when type=MERGE_RSP
            internal string subGroup_name = null; // to identify the subgroup of the current member.
            internal bool isStartedAsMirror = false;  // to identify the current memebr as active or mirror. 
            internal ArrayList nodeList;//nodes to which this can not establish the connection.
            internal object arg;
            internal string gms_id;

            public HDR()
            {
            } // used for Externalization

            public HDR(byte type)
            {
                this.type = type;
            }

            public HDR(byte type, object argument)
            {
                this.type = type;
                this.arg = argument;
            }

            /// <summary>Used for VIEW header </summary>
            public HDR(byte type, View view)
            {
                this.type = type;
                this.view = view;
            }


            /// <summary>Used for JOIN_REQ or LEAVE_REQ header </summary>
            public HDR(byte type, Address mbr)
            {
                this.type = type;
                this.mbr = mbr;
            }

            /// <summary>Used for JOIN_REQ or LEAVE_REQ header </summary>
            public HDR(byte type, Address mbr, string subGroup_name)
            {
                this.type = type;
                this.mbr = mbr;
                this.subGroup_name = subGroup_name;
            }

            /// <summary>Used for JOIN_REQ header </summary>
            public HDR(byte type, Address mbr, string subGroup_name, bool isStartedAsMirror)
            {
                this.type = type;
                this.mbr = mbr;
                this.subGroup_name = subGroup_name;
                this.isStartedAsMirror = isStartedAsMirror;
            }
            /// <summary>Used for JOIN_RSP header </summary>
            public HDR(byte type, JoinRsp join_rsp)
            {
                this.type = type;
                this.join_rsp = join_rsp;
            }

            public string GMSId
            {
                get { return gms_id; }
                set { gms_id = value; }
            }

            public override string ToString()
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder("HDR");
                sb.Append('[' + type2String(type) + ']');
                switch (type)
                {


                    case JOIN_REQ:
                        sb.Append(": mbr=" + mbr);
                        break;

                    case SPECIAL_JOIN_REQUEST:
                        sb.Append(": mbr=" + mbr);
                        break;

                    case RE_CHECK_CLUSTER_HEALTH:
                        sb.Append(": mbr=" + mbr);
                        break;

                    case JOIN_RSP:
                        sb.Append(": join_rsp=" + join_rsp);
                        break;


                    case LEAVE_REQ:
                        sb.Append(": mbr=" + mbr);
                        break;


                    case LEAVE_RSP:
                        break;


                    case VIEW:
                        sb.Append(": view=" + view);
                        break;


                    case MERGE_REQ:
                        sb.Append(": merge_id=" + merge_id);
                        break;


                    case MERGE_RSP:
                        sb.Append(": view=" + view + ", digest=" + digest + ", merge_rejected=" + merge_rejected + ", merge_id=" + merge_id);
                        break;


                    case INSTALL_MERGE_VIEW:
                        sb.Append(": view=" + view + ", digest=" + digest);
                        break;


                    case CANCEL_MERGE:
                        sb.Append(", <merge cancelled>, merge_id=" + merge_id);
                        break;

                    case CONNECTION_BROKEN:
                        sb.Append("<suspected member : " + mbr + " >");
                        break;

                    case VIEW_REJECTED:
                        sb.Append("<rejected by : " + mbr + " >");

                        break;

                    case INFORM_NODE_REJOINING:
                        sb.Append("INFORM_NODE_REJOINING");
                        break;

                    case RESET_ON_NODE_REJOINING:
                        sb.Append("RESET_ON_NODE_REJOINING");
                        break;

                    case VIEW_RESPONSE:
                        sb.Append("VIEW_RESPONSE");
                        break;

                    case IS_NODE_IN_STATE_TRANSFER:
                        sb.Append("IS_NODE_IN_STATE_TRANSFER");
                        break;

                    case IS_NODE_IN_STATE_TRANSFER_RSP:
                        sb.Append("IS_NODE_IN_STATE_TRANSFER_RSP->" + arg);
                        break;

                    case INFORM_ABOUT_NODE_DEATH:
                        sb.Append("INFORM_ABOUT_NODE_DEATH (" + arg + ")");
                        break;
                }
                sb.Append('\n');
                return sb.ToString();
            }


            public static string type2String(int type)
            {
                switch (type)
                {

                    case JOIN_REQ:
                        return "JOIN_REQ";

                    case SPECIAL_JOIN_REQUEST:
                        return "SPECIAL_JOIN_REQUEST";

                    case JOIN_RSP:
                        return "JOIN_RSP";

                    case LEAVE_REQ:
                        return "LEAVE_REQ";

                    case LEAVE_RSP:
                        return "LEAVE_RSP";

                    case VIEW:
                        return "VIEW";

                    case MERGE_REQ:
                        return "MERGE_REQ";

                    case MERGE_RSP:
                        return "MERGE_RSP";

                    case INSTALL_MERGE_VIEW:
                        return "INSTALL_MERGE_VIEW";

                    case CANCEL_MERGE:
                        return "CANCEL_MERGE";

                    case CAN_NOT_CONNECT_TO:
                        return "CAN_NOT_CONNECT_TO";

                    case LEAVE_CLUSTER:
                        return "LEAVE_CLUSTER";

                    case CONNECTION_BROKEN:
                        return "CONNECTION_BROKEN";

                    case CONNECTED_NODES_REQUEST:
                        return "CONNECTED_NODES_REQUEST";

                    case CONNECTED_NODES_RESPONSE:
                        return "CONNECTED_NODES_RESPONSE";

                    case VIEW_REJECTED:
                        return "VIEW_REJECTED";

                    case RE_CHECK_CLUSTER_HEALTH:
                        return "RE_CHECK_CLUSTER_HEALTH";

                    case INFORM_ABOUT_NODE_DEATH:
                        return "RE_CHECK_CLUSTER_HEALTH";

                    case IS_NODE_IN_STATE_TRANSFER:
                        return "IS_NODE_IN_STATE_TRANSFER";

                    case IS_NODE_IN_STATE_TRANSFER_RSP:
                        return "IS_NODE_IN_STATE_TRANSFER_RSP";


                    default:
                        return "<unknown>";

                }
            }

            #region ICompactSerializable Members

            void ICompactSerializable.Deserialize(CompactReader reader)
            {
                type = reader.ReadByte();
                view = View.ReadView(reader);
                mbr = Address.ReadAddress(reader);
                join_rsp = (JoinRsp)reader.ReadObject();
                digest = (Digest)reader.ReadObject();
                merge_id = reader.ReadObject();
                merge_rejected = reader.ReadBoolean();
                subGroup_name = reader.ReadString();
                nodeList = reader.ReadObject() as ArrayList;
                arg = reader.ReadObject();
                isStartedAsMirror = reader.ReadBoolean();
                gms_id = reader.ReadObject() as string;
            }

            void ICompactSerializable.Serialize(CompactWriter writer)
            {
                writer.Write(type);
                View.WriteView(writer, view);
                Address.WriteAddress(writer, mbr);
                writer.WriteObject(join_rsp);
                writer.WriteObject(digest);
                writer.WriteObject(merge_id);
                writer.Write(merge_rejected);
                writer.Write(subGroup_name);
                writer.WriteObject(nodeList);
                writer.WriteObject(arg);
                writer.Write(isStartedAsMirror);
                writer.WriteObject(gms_id);
            }

            #endregion
        }
    }
}
