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
// $Id: CoordGmsImpl.java,v 1.13 2004/09/08 09:17:17 belaban Exp $
namespace Alachisoft.NGroups.Protocols.pbcast
{
    /// <summary> Starts the merge protocol (only run by the merge leader). Essentially sends a MERGE_REQ to all
    /// coordinators of all subgroups found. Each coord receives its digest and view and returns it.
    /// The leader then computes the digest and view for the new group from the return values. Finally, it
    /// sends this merged view/digest to all subgroup coordinators; each coordinator will install it in their
    /// subgroup.
    /// </summary>
    internal class MergeTask : IThreadRunnable
    {
        public MergeTask(CoordGmsImpl enclosingInstance)
        {
            InitBlock(enclosingInstance);
        }
        private void InitBlock(CoordGmsImpl enclosingInstance)
        {
            this.enclosingInstance = enclosingInstance;
        }
        private CoordGmsImpl enclosingInstance;
        virtual public bool Running
        {
            get
            {
                return t != null && t.IsAlive;
            }

        }
        public CoordGmsImpl Enclosing_Instance
        {
            get
            {
                return enclosingInstance;
            }

        }
        internal ThreadClass t = null;
        internal System.Collections.ArrayList coords = null; // list of subgroup coordinators to be contacted

        public virtual void start(System.Collections.ArrayList coords)
        {
            if (t == null)
            {
                this.coords = coords;
                t = new ThreadClass(new System.Threading.ThreadStart(this.Run), "MergeTask thread");
                t.IsBackground = true;
                t.Start();
            }
        }

        public virtual void stop()
        {
            ThreadClass tmp = t;
            if (Running)
            {
                t = null;
                tmp.Interrupt();
            }
            t = null;
            coords = null;
        }

        /// <summary> Runs the merge protocol as a leader</summary>
        public virtual void Run()
        {
            MergeData combined_merge_data = null;

            if (Enclosing_Instance.merging == true)
            {
                Enclosing_Instance.gms.Stack.NCacheLog.Warn("CoordGmsImpl.Run()", "merge is already in progress, terminating");
                return;
            }

            Enclosing_Instance.gms.Stack.NCacheLog.Debug("CoordGmsImpl.Run()", "merge task started");
            try
            {

                /* 1. Generate a merge_id that uniquely identifies the merge in progress */
                Enclosing_Instance.merge_id = Enclosing_Instance.generateMergeId();

                /* 2. Fetch the current Views/Digests from all subgroup coordinators */
                Enclosing_Instance.getMergeDataFromSubgroupCoordinators(coords, Enclosing_Instance.gms.merge_timeout);

                /* 3. Remove rejected MergeData elements from merge_rsp and coords (so we'll send the new view only
                to members who accepted the merge request) */
                Enclosing_Instance.removeRejectedMergeRequests(coords);

                if (Enclosing_Instance.merge_rsps.Count <= 1)
                {
                    Enclosing_Instance.gms.Stack.NCacheLog.Warn("CoordGmsImpl.Run()", "merge responses from subgroup coordinators <= 1 (" + Global.CollectionToString(Enclosing_Instance.merge_rsps) + "). Cancelling merge");
                    Enclosing_Instance.sendMergeCancelledMessage(coords, Enclosing_Instance.merge_id);
                    return;
                }

                /* 4. Combine all views and digests into 1 View/1 Digest */
                combined_merge_data = Enclosing_Instance.consolidateMergeData(Enclosing_Instance.merge_rsps);
                if (combined_merge_data == null)
                {
                    Enclosing_Instance.gms.Stack.NCacheLog.Error("CoordGmsImpl.Run()", "combined_merge_data == null");
                    Enclosing_Instance.sendMergeCancelledMessage(coords, Enclosing_Instance.merge_id);
                    return;
                }

                /* 5. Send the new View/Digest to all coordinators (including myself). On reception, they will
                install the digest and view in all of their subgroup members */
                Enclosing_Instance.sendMergeView(coords, combined_merge_data);
            }
            catch (System.Exception ex)
            {
                Enclosing_Instance.gms.Stack.NCacheLog.Error("MergeTask.Run()", ex.ToString());
            }
            finally
            {
                Enclosing_Instance.merging = false;

                Enclosing_Instance.gms.Stack.NCacheLog.Debug("CoordGmsImpl.Run()", "merge task terminated");
                t = null;
            }
        }
    }

}
