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
// $Id: GroupRequest.java,v 1.8 2004/09/05 04:54:22 ovidiuf Exp $
using Alachisoft.NGroups.Blocks;
using Alachisoft.NCache.Common.Net;
using System.Collections;

namespace Alachisoft.NGroups
{
    internal class Reaper : IThreadRunnable
    {
        private void InitBlock(ConnectionTable enclosingInstance)
        {
            this.enclosingInstance = enclosingInstance;
        }
        private ConnectionTable enclosingInstance;
        virtual public bool Running
        {
            get
            {
                return t != null;
            }

        }
        public ConnectionTable Enclosing_Instance
        {
            get
            {
                return enclosingInstance;
            }

        }
        internal ThreadClass t = null;
        private string _cacheName;

        internal Reaper(ConnectionTable enclosingInstance)
        {
            InitBlock(enclosingInstance);
        }

        public virtual void start()
        {
            if (Enclosing_Instance.conns_NIC_1.Count == 0)
                return;
            if (t != null && !t.IsAlive)
                t = null;
            if (t == null)
            {
                //RKU 7.4.2003, put in threadgroup
                t = new ThreadClass(new System.Threading.ThreadStart(this.Run), "ConnectionTable.ReaperThread");
                t.IsBackground = true; // will allow us to terminate if all remaining threads are daemons
                t.Start();
            }
        }

        public virtual void stop()
        {
            if (t != null)
                t = null;
        }

        public virtual void Run()
        {
            Connection value_Renamed;
            System.Collections.DictionaryEntry entry;
            long curr_time;
            ArrayList temp = new ArrayList();

            if (enclosingInstance.NCacheLog.IsInfoEnabled) enclosingInstance.NCacheLog.Info("connection reaper thread was started. Number of connections=" + Enclosing_Instance.conns_NIC_1.Count + ", reaper_interval=" + Enclosing_Instance.reaper_interval + ", conn_expire_time=" + Enclosing_Instance.conn_expire_time);

            while (Enclosing_Instance.conns_NIC_1.Count > 0 && t != null)
            {
                // first sleep
                Util.Util.sleep(Enclosing_Instance.reaper_interval);

                if (enclosingInstance.NCacheLog.IsInfoEnabled) enclosingInstance.NCacheLog.Info("ConnectionTable.Reaper", "b4 lock conns.SyncRoot");
                lock (Enclosing_Instance.conns_NIC_1.SyncRoot)
                {
                    curr_time = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
                    for (System.Collections.IEnumerator it = Enclosing_Instance.conns_NIC_1.GetEnumerator(); it.MoveNext();)
                    {
                        entry = (System.Collections.DictionaryEntry)it.Current;
                        value_Renamed = (Connection)entry.Value;

                        if (enclosingInstance.NCacheLog.IsInfoEnabled) enclosingInstance.NCacheLog.Info("connection is " + ((curr_time - value_Renamed.last_access) / 1000) + " seconds old (curr-time=" + curr_time + ", last_access=" + value_Renamed.last_access + ')');
                        if (value_Renamed.last_access + Enclosing_Instance.conn_expire_time < curr_time)
                        {
                            if (enclosingInstance.NCacheLog.IsInfoEnabled) enclosingInstance.NCacheLog.Info("connection " + value_Renamed + " has been idle for too long (conn_expire_time=" + Enclosing_Instance.conn_expire_time + "), will be removed");
                            value_Renamed.Destroy();
                            temp.Add(it.Current);
                        }
                    }

                    // Now  remove closed connection from the connection hashtable

                    for (int i = 0; i < temp.Count; i++)
                    {
                        if (Enclosing_Instance.conns_NIC_1.Contains((Address)temp[i]))
                        {
                            Enclosing_Instance.conns_NIC_1.Remove((Address)temp[i]);
                            temp[i] = null;
                        }
                    }

                }
                if (enclosingInstance.NCacheLog.IsInfoEnabled) enclosingInstance.NCacheLog.Info("ConnectionTable.Reaper", "after lock conns.SyncRoot");
            }

            if (enclosingInstance.NCacheLog.IsInfoEnabled) enclosingInstance.NCacheLog.Info("reaper terminated");
            t = null;
        }
    }
}
