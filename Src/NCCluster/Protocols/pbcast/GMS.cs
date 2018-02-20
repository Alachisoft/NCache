// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// $Id: GMS.java,v 1.17 2004/09/03 12:28:04 belaban Exp $
using System;
using System.Threading;
using System.Collections;
using Alachisoft.NGroups.Util;
using Alachisoft.NGroups.Stack;
using Alachisoft.NCache.Common.Mirroring;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Enum;

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

        private object join_mutex = new object();//Stops simultaneous node joining

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
                    for (System.Collections.IEnumerator it = old_mbrs.GetEnumerator(); it.MoveNext();)
                    {
                        Address addr = (Address)it.Current;
                        if (!leaving.Contains(addr))
                            leaving.Add(addr);
                    }
                }
                if (suspected_mbrs != null)
                {
                    for (System.Collections.IEnumerator it = suspected_mbrs.GetEnumerator(); it.MoveNext();)
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

            // next view: current mbrs + new_mbrs - old_mbrs - suspected_mbrs
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
            GmsHDR hdr;

            Stack.NCacheLog.Debug("pb.GMS.castViewChange()", "mcasting view {" + new_view + "} (" + new_view.size() + " mbrs)\n");

            view_change_msg = new Message(); // bcast to all members
            

            hdr = new GmsHDR(GmsHDR.VIEW, new_view);
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
            GmsHDR hdr = new GmsHDR(GmsHDR.VIEW_RESPONSE, true);
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
                        //Console.WriteLine("=== gms.localAddr = " + local_addr.ToString() + " --> " + newCoordinator.ToString() + " --> " + currentCoodinator.ToString());
                        Stack.NCacheLog.CriticalInfo("GMS.InstallView", "Force Join Cluster");
                        if (!new_view.ForceInstall)
                        {
                            if (!VerifySuspect(currentCoodinator))
                            {
                                Stack.NCacheLog.Error("GMS.installView", "rejecting the view from " + newCoordinator + " as my own coordinator[" + currentCoodinator + "] is not down");
                                Event viewEvt = new Event(Event.VIEW_CHANGE_OK, null, Priority.High);
                                passDown(viewEvt);

                                //we should inform the coordinator of this view that i can't be the member
                                //of your view as my own coordinator is alive.

                                Message msg = new Message(new_view.Coordinator, null, new byte[0]);
                                msg.putHeader(HeaderType.GMS, new GmsHDR(GmsHDR.VIEW_REJECTED, local_addr));
                                passDown(new Event(Event.MSG, msg, Priority.High));

                                return;
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
                    
                    if (view_id != null)
                    {
                        Membership newMembers = new Membership(mbrs);
                        if (members.Equals(newMembers) && vid.CoordAddress.Equals(view_id.CoordAddress))
                        {

                            Stack.NCacheLog.Error("GMS.InstallView", "[" + local_addr + "] received view has the same members as current view;" + " discarding it (current vid: " + view_id + ", new vid: " + vid + ')');
                            Event viewEvt = new Event(Event.VIEW_CHANGE_OK, null, Priority.High);

                           
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
                        for (System.Collections.IEnumerator it = mbrs.GetEnumerator(); it.MoveNext();)
                        {
                            Address addr = (Address)it.Current;
                            if (!prev_members.contains(addr))
                                prev_members.add(addr);
                        }
                    }

                    // Send VIEW_CHANGE event up and down the stack:
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("GMS.installView", "broadcasting view change within stack");

                    //passUp(view_event);

                    coord = determineCoordinator();
                    
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
            GmsHDR hdr;
            MergeData merge_data;

            switch (evt.Type)
            {


                case Event.MSG:
                    msg = (Message)evt.Arg;

                    obj = msg.getHeader(HeaderType.GMS);
                    if (obj == null || !(obj is GmsHDR))
                        break;
                    hdr = (GmsHDR)msg.removeHeader(HeaderType.GMS);
                    switch (hdr.type)
                    {

                        case GmsHDR.JOIN_REQ:
                            object[] args = new object[4];
                            args[0] = hdr.mbr;
                            args[1] = hdr.subGroup_name;
                            args[2] = hdr.GMSId;
                            ThreadPool.QueueUserWorkItem(new WaitCallback(handleJoinrequestAsync), args);                            
                            break;

                        case GmsHDR.SPECIAL_JOIN_REQUEST:
                            HandleSpecialJoinRequest(hdr.mbr, hdr.GMSId);
                            break;

                        case GmsHDR.JOIN_RSP:
                            MarkStateTransferInProcess();
                            impl.handleJoinResponse(hdr.join_rsp);
                            break;

                        case GmsHDR.LEAVE_REQ:
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

                        case GmsHDR.LEAVE_RSP:
                            impl.handleLeaveResponse();
                            break;

                        case GmsHDR.VIEW_RESPONSE:
                            if (_promise != null)
                                _promise.SetResult(hdr.arg);
                            break;

                        case GmsHDR.VIEW:
                            if (hdr.view == null)
                            {
                                Stack.NCacheLog.Error("[VIEW]: view == null");
                                return;
                            }
                            else
                                Stack.NCacheLog.CriticalInfo("GMS.Up", "received view from :" + msg.Src + " ; view = " + hdr.view);
                            impl.handleViewChange(hdr.view, hdr.digest);
                            break;


                        case GmsHDR.MERGE_REQ:
                            impl.handleMergeRequest(msg.Src, hdr.merge_id);
                            break;


                        case GmsHDR.MERGE_RSP:
                            merge_data = new MergeData(msg.Src, hdr.view, hdr.digest);
                            merge_data.merge_rejected = hdr.merge_rejected;
                            impl.handleMergeResponse(merge_data, hdr.merge_id);
                            break;


                        case GmsHDR.INSTALL_MERGE_VIEW:
                            impl.handleMergeView(new MergeData(msg.Src, hdr.view, hdr.digest), hdr.merge_id);
                            break;


                        case GmsHDR.CANCEL_MERGE:
                            impl.handleMergeCancelled(hdr.merge_id);
                            break;

                        case GmsHDR.CAN_NOT_CONNECT_TO:
                            impl.handleCanNotConnectTo(msg.Src, hdr.nodeList);
                            break;

                        case GmsHDR.LEAVE_CLUSTER:                            
                            string gmsId = hdr.arg as string;//reported gms id
                            string myGmsId = GetNodeGMSId(local_addr);

                            if (gmsId != null && myGmsId != null && gmsId.Equals(myGmsId))
                            {
                                ThreadPool.QueueUserWorkItem(new WaitCallback(handleLeaveClusterRequestAsync), hdr.mbr);
                            }
                            break;

                        case GmsHDR.CONNECTION_BROKEN:
                            impl.handleConnectionBroken(msg.Src, hdr.mbr);
                            break;

                        case GmsHDR.VIEW_REJECTED:
                            impl.handleViewRejected(hdr.mbr);
                            break;

                        case GmsHDR.INFORM_NODE_REJOINING:
                            impl.handleInformNodeRejoining(msg.Src, hdr.mbr);
                            break;

                        case GmsHDR.RESET_ON_NODE_REJOINING:
                            impl.handleResetOnNodeRejoining(msg.Src, hdr.mbr, hdr.view);
                            break;

                        case GmsHDR.RE_CHECK_CLUSTER_HEALTH:                            
                            Thread t = new Thread(new ParameterizedThreadStart(impl.ReCheckClusterHealth));
                            t.Start(hdr.mbr);
                            break;

                        case GmsHDR.INFORM_ABOUT_NODE_DEATH:
                            //Replica is not supposed to handle this event
                            if (isPartReplica && _startedAsMirror) break;

                            impl.handleInformAboutNodeDeath(msg.Src, (Address)hdr.arg);
                            break;

                        case GmsHDR.IS_NODE_IN_STATE_TRANSFER:
                            impl.handleIsClusterInStateTransfer(msg.Src);
                            break;

                        case GmsHDR.IS_NODE_IN_STATE_TRANSFER_RSP:
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
                    //stack.nTrace.criticalInfo("Gms.Up", evt.ToString());
                    impl.handleConnectionFailure(evt.Arg as ArrayList);
                    return;//dont passup

                case Event.NODE_REJOINING:
                    //stack.nTrace.criticalInfo("Gms.Up", evt.ToString());
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
                GmsHDR hdr = new GmsHDR(GmsHDR.INFORM_ABOUT_NODE_DEATH);
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
            //Stack.NCacheLog.DevTrace("gms.IsClusterInStateTransfer", "start ->" + _stateTransferInProcess);
            if (GetStateTransferStatus()) return true;
            //check with other members
            //Stack.NCacheLog.DevTrace("gms.IsClusterInStateTransfer", "sending to nodes  ->" + members.Members.Count);
            if (this.members != null)
            {
                ArrayList allmembers = this.members.Members;
                if (allmembers != null && allmembers.Count > 0)
                {
                    _stateTransferPromise = new SateTransferPromise(allmembers.Count);
                    Message msg = new Message(null, null, new byte[0]);
                    msg.Dests = allmembers;
                    GmsHDR hdr = new GmsHDR(GmsHDR.IS_NODE_IN_STATE_TRANSFER);
                    msg.putHeader(HeaderType.GMS, hdr);
                    down(new Event(Event.MSG, msg, Priority.High));
                    Object objectState = _stateTransferPromise.WaitResult(_stateTransferQueryTimesout);
                
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
                    impl.join(local_addr);
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
                    bool twoPhaseConnect = false;
                    try
                    {
                        object[] addrs = (object[])evt.Arg;
                        group_addr = (string)addrs[0];
                        subGroup_addr = (string)addrs[1];

                        twoPhaseConnect = (bool)addrs[2];
                    }
                    catch (System.InvalidCastException e)
                    {
                        Stack.NCacheLog.Error("[CONNECT]: group address must be a string (channel name)", e.Message);
                    }
                    if (local_addr == null)
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("[CONNECT] local_addr is null");
                    if (!twoPhaseConnect)
                        impl.join(local_addr);

                    //Console.WriteLine("Connect OK GMS");
                    passUp(new Event(Event.CONNECT_OK));
                    return; // don't pass down: was already passed down


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
                            //Stack.NCacheLog.DevTrace("POR.Init", "returning");
                            return;
                        }
                    }
                    //This will always be true but to be sure

                    if (haveCoordinatorRole())
                    {
                        CoordGmsImpl coordImpl = impl as CoordGmsImpl;
                        if (coordImpl != null)
                            if (!coordImpl.CheckOwnClusterHealth(isPOR, retryNumber))
                            {
                                //Console.WriteLine("CLuster was at fault");
                                //becomeClient();
                                //ClientGmsImpl clientGMS = impl as ClientGmsImpl;
                                //clientGMS.join(this.local_addr, false);

                            }
                    }
                    //}
                    //Console.WriteLine("Retruning Confirm Cluster Startup");
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
                        //Stack.NCacheLog.CriticalInfo("pbcast.GMS.down()", "Ask join response received.");
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

        internal void acquireHashmap(System.Collections.ArrayList mbrs, bool isJoining, string subGroup)
        {
            int maxTries = 3;
        
            lock (_mapMainMutex)
            {

                lock (acquireMap_mutex)
                {
                    
                    Event evt = new Event();
                    evt.Type = Event.HASHMAP_REQ;
                    evt.Arg = new object[] { mbrs, isJoining, subGroup };
                    _hashmap = null; //Reseting because it will be set by the down() method of GMS when upper layer will give the new hashmap
                    lock (evt)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(sendUp), evt);
                        Monitor.Wait(evt);
                    }
                    Stack.NCacheLog.CriticalInfo("GMS.AcquireHashmap", ("") + "request the caching layer for hashmap.");
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("pbcast.GMS.acquireHashmap()", ("") + " request the caching layer for hashmap");
                    //we wait for maximum 3 seconds to acquire a hashmap ( 3 * 3[retries]] = 9 seconds MAX)
                    do
                    {

                        Stack.NCacheLog.CriticalInfo("GMS.AcquireHashmap", "Going to wait on acquireMap_mutex try->" + maxTries.ToString());
                        bool acquired = System.Threading.Monitor.Wait(acquireMap_mutex, 3000);
                        Stack.NCacheLog.CriticalInfo("GMS.AcquireHashmap", "Return from wait on acquireMap_mutex try->" + maxTries.ToString());

                        if (_hashmap != null)
                        {
                            //Stack.NCacheLog.CriticalInfo("pbcast.GMS.acquireHashmap()", "hashmap:" + _hashmap.ToString());
                            break;
                        }

                        maxTries--;
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("pbcast.GMS.acquireHashmap()", ("") + "null map received... requesting the hashmap again");

                    } while (maxTries > 0);
                }
            }

            if (maxTries < 0 && _hashmap == null)
            {
                Stack.NCacheLog.Error("GMS.AcquireHashmap", "Hashmap acquisition failure for :" + Global.CollectionToString(mbrs) + " joining? " + isJoining);
            }
            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("pbcast.GMS.acquireHashmap()", ("") + "request for hashmap end");
        }

        internal bool allowJoin(Address mbr)
        {
            
            if (!isPartReplica) return true;
          
            lock (acquirePermission_mutex)
            {
                _allowJoin = false;


                ArrayList existingMembers = members.Members;
                Address lastJoiney = null;
                if (existingMembers.Count > 0)
                {
                    lastJoiney = existingMembers[existingMembers.Count - 1] as Address;
                    if (existingMembers.Count > 1)
                    {
                        Address secondLastJoinee = existingMembers[existingMembers.Count - 2] as Address;
                        if (!lastJoiney.IpAddress.Equals(secondLastJoinee.IpAddress))
                            return false;
                    }
                    else
                        return false;

                }

                Stack.NCacheLog.CriticalInfo("GMS.AllowJoin", "Join permission for " + mbr.ToString());
                bool inStateTransfer = IsClusterInStateTransfer();
                _allowJoin = !inStateTransfer;

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

            //Console.WriteLine("pbcast.GMS.HandleSpecialJoinRequest() = " + local_addr.ToString());

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
            //Console.WriteLine("New Coord = " + mbrs[0]);

            for (int i = 0; i < mbrs.Count - 1; i++)
            {
                new_coord = (Address)mbrs[i];
                if (local_addr.Equals(new_coord))
                {
                    //Console.WriteLine("New Coord = " + local_addr.ToString());
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
            handleJoinRequest(arr[0] as Address, arr[1] as string, arr[2] as string);
        }

        internal void handleSuspectAsync(object mbr)
        {
            impl.handleSuspect(mbr as Address);
        }

        internal void handleLeaveClusterRequestAsync(object mbr)
        {
            impl.handleLeaveClusterRequest(mbr as Address);
        }

        internal virtual void handleJoinRequest(Address mbr, string subGroup_name, string gmsId)
        {
            //Console.WriteLine("handleJoinRequest -> " + mbr.ToString());

            JoinRsp join_rsp = null;
            Message m;
            GmsHDR hdr;

            if (mbr == null)
            {
                Stack.NCacheLog.Error("mbr is null");
                return;
            }

            Stack.NCacheLog.Debug("pbcast.GMS.handleJoinRequest()", "mbr=" + mbr);

            lock (join_mutex)
            {

                if (_nodeJoiningInProgress || _isLeavingInProgress || (_isStarting && !local_addr.IpAddress.Equals(mbr.IpAddress)))
                {
                    Stack.NCacheLog.CriticalInfo("GMS.HandleJoinRequest", "node :" + mbr + "joining is in progress.");


                    join_rsp = new JoinRsp(null, null);
                    join_rsp.JoinResult = JoinResult.MembershipChangeAlreadyInProgress;


                    m = new Message(mbr, null, null);
                    hdr = new GmsHDR(GmsHDR.JOIN_RSP, join_rsp);
                    m.putHeader(HeaderType.GMS, hdr);
                    passDown(new Event(Event.MSG, m, Priority.High));
                    return;
                }
                else
                {

                    _nodeJoiningInProgress = true;
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
                    hdr = new GmsHDR(GmsHDR.JOIN_RSP, join_rsp);
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
                    hdr = new GmsHDR(GmsHDR.JOIN_RSP, join_rsp);
                    m.putHeader(HeaderType.GMS, hdr);
                    passDown(new Event(Event.MSG, m, Priority.High));
                    lock (join_mutex)
                    {
                        _nodeJoiningInProgress = false;
                    }
                    return;
                }
            }



            if (allowJoin(mbr))
            {
                Stack.NCacheLog.Debug("pbcast.GMS.handleJoinRequest()", " joining allowed");
                bool acauireHashmap = true;

                join_rsp = impl.handleJoin(mbr, subGroup_name, gmsId, ref acauireHashmap);

                if (join_rsp == null)
                    Stack.NCacheLog.Error("pbcast.GMS.handleJoinRequest()", impl.GetType().ToString() + ".handleJoin(" + mbr + ") returned null: will not be able to multicast new view");

                
                System.Collections.ArrayList mbrs = new System.Collections.ArrayList(1);
                mbrs.Add(mbr);

              
                //some time coordinator gms impl returns the same existing view in join response. 
                //we dont need to acquire the hashmap again in this case coz that hashmap has already been acquired.
                if (acauireHashmap)
                    acquireHashmap(mbrs, true, subGroup_name);

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
            hdr = new GmsHDR(GmsHDR.JOIN_RSP, join_rsp);
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

     
    }
}
