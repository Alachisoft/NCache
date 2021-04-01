//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
#if !NETCORE
using System;
using System.Collections;
using System.Text;
using System.Management.Instrumentation;
using System.Management;
using System.Windows.Forms;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Instrumentation
{
    [InstrumentationClass(InstrumentationType.Instance)]
    public class ClusterStats : System.Management.Instrumentation.Instrumentation
    {
        internal string             _clusterName;               
        private string              _cacheScheme;
        private string              _coordinator;
        private ulong               _totalCount;               //Item count at Cluster level
        private DateTime            _clusterUpTime;             //The time at which the cluster was started
        private int                _port;                       //Cluster Port
        private object _syncPoint = new object();
        private ArrayList _nodes = new ArrayList();           //List of server Nodes
        private ArrayList _runningNodes = new ArrayList();      //List of Running Nodes
        private ArrayList _porNodes = new ArrayList();
        private ArrayList _inprocInstances = new ArrayList();
        private Hashtable _inprocPorInstances = new Hashtable();
        private Hashtable _porRunningNodeList = new Hashtable();

        private bool _doNotPublish = false;
        /// <summary>
        /// 
        /// </summary>
        public ClusterStats()
        {
            try
            {
                this.Publish();
            }
            catch { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DoNotPublish"></param>
        public ClusterStats(bool DoNotPublish)
        {
            _doNotPublish = true;
        }


        public void PublishInsctance()
        {
            try
            {
                _doNotPublish = false;

                /* Commented to stop posting on WMI */
                this.Publish();
            }
            catch { }
        }

        /// <summary>
        /// Public member variable declaration that are to be published over the network through WMI
        /// </summary>
        #region Public Members

        /// <summary>
        /// Name of the Cluster
        /// </summary>
        public string ClusterName
        {
            get
            {
                return _clusterName;
            }
            set
            {
                _clusterName = value;
            }
        }

        /// <summary>
        /// Current coordinator
        /// </summary>
        public string Coordinator
        {
            get 
            {
                return this._coordinator; 
            }
            set 
            {
                this._coordinator = value; 
            }
        }

        /// <summary>
        /// Time at which the Cluster was started
        /// </summary>
        public DateTime ClusterUpTime
        {
            get
            {
                return _clusterUpTime;
            }

            set
            {
                _clusterUpTime = value;
            }
        }

        /// <summary>
        /// Cache scheme i.e replicated, partitioned etc
        /// </summary>
        public string CacheScheme
        {
            get
            {
                return _cacheScheme;
            }

            set
            {
                _cacheScheme = value;
            }
        }

        /// <summary>
        /// function supposed to publish or post curretn object over the WMI
        /// </summary>
        /// <returns></returns>
        public bool Publish()
        {
            try
            {

                /* Commented to stop posting on WMI */
                System.Management.Instrumentation.Instrumentation.Publish(this);
                return true;
            }
            catch (Exception)
            {
                throw;
                //return false;
            }

        }

        /// <summary>
        /// Total # of items in the Cluster
        /// </summary>
        [CLSCompliant(false)]
        public ulong ItemCount
        {
            get
            {
                return _totalCount;
            }
            set
            {
                _totalCount = value;
            }
        }

        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                _port = value;
            }
        }
        /// <summary>
        /// Total number of Server nodes in the cluster
        /// </summary>
        public int NodeCount
        {
            get
            {
                return _nodes.Count;
            }
        }

        /// <summary>
        /// List containing names of the Server nodes in the cluster
        /// </summary>
        public string[] Nodes
        {
            set
            {
                for (int i = 0; i < _nodes.Count; i++)
                {
                    _nodes[i] = _nodes[i];    //as WMI do not support collections so public member is converted into WMI supported value
                }
            }

            get
            {
                string[] nodeslist = new string[_nodes.Count];//as WMI do not support collections so public member is converted into WMI supported value
                for (int i = 0; i < _nodes.Count; i++)
                {
                    nodeslist[i] = _nodes[i].ToString();
                }
                _nodes.CopyTo(nodeslist);

                return nodeslist;
            }
        }

        public string[] ReplicatedPartitions
        {
            get
            {
                string[] nodeslist = new string[_porNodes.Count];//as WMI do not support collections so public member is converted into WMI supported value
                for (int i = 0; i < _porNodes.Count; i++)
                {
                    nodeslist[i] = _porNodes[i].ToString();
                }
                return nodeslist;
            }
        }

        /// <summary>
        /// list of Active Server Nodes
        /// </summary>
        public string[] RunningNodes
        {
            set
            {
                for (int i = 0; i < _runningNodes.Count; i++)
                {
                    _nodes[i] = _runningNodes[i];    //as WMI do not support collections so public member is converted into WMI supported value
                }
            }

            get
            {
                string[] nodeslist = new string[_runningNodes.Count];      //as WMI do not support collections so public member is converted into WMI supported value
                for (int i = 0; i < _runningNodes.Count; i++)
                {
                    nodeslist[i] = _runningNodes[i].ToString();
                }
                return nodeslist;
            }
        }
        #endregion

        /// <summary>
        /// populating the list of nodes on some server node if some nodes already exists in the cluster
        /// </summary>
        /// <param name="node_list">list of currently present nodes</param>
        public void PopulateNodes(string initialhostlist)
        {
            try
            {
                string[] nodes = initialhostlist.Split(',');
                _nodes.Clear();
                foreach (string node in nodes)
                {
                    string[] split = node.Split('[');
                    AddNode(split[0].Trim());
                }
                foreach (string node in this._inprocInstances)
                {
                    AddNode(node);
                }
            }
            catch { }
        }

        public void PopulateNodes(Hashtable initialhoslList)
        {
            if (initialhoslList == null) return;
            try
            {
                _porNodes = new ArrayList();
                IDictionaryEnumerator en = initialhoslList.GetEnumerator();
                while (en.MoveNext())
                {
                    string inprocPorInstances = this._inprocPorInstances[en.Key] as string;
                    if (inprocPorInstances != null && inprocPorInstances != string.Empty)
                    {
                        inprocPorInstances = "," + inprocPorInstances;
                    }
                    AddPorNode((string)en.Key + ":" + (string)en.Value + inprocPorInstances);
                }
            }
            catch { }
        }
        /// <summary>
        /// Called each time a new server node is set to running state
        /// </summary>
        /// <param name="NodeName">Name of the Node</param>
        public void MemberJoined(string port, string nodeName, bool raiseEvent, bool isInproc)
        {
            try
            {
                if (isInproc && !this._inprocInstances.Contains(nodeName.Trim() + "." + port))
                {
                    nodeName = nodeName.Trim() + "." + port;
                    this._inprocInstances.Add(nodeName);
                    if (!this._nodes.Contains(nodeName)) this._nodes.Add(nodeName);
                }
                if (_runningNodes.IndexOf(nodeName.Trim()) == -1)
                {
                    _runningNodes.Add(nodeName.Trim());

                    if (raiseEvent)
                    {
                        NodeUp node = new NodeUp(this.ClusterName, nodeName.Trim());
                        node.Fire();
                    }
                }
            }
            catch { }
        }


        public void MemberJoined(string port, string nodeName, string subGroupName, bool fireEvent, bool isInproc)
        {

            try
            {
                if (isInproc)
                {
                    nodeName = nodeName.Trim() + "." + port;
                    if (this._inprocPorInstances.Contains(subGroupName))
                    {
                        string previousInstances = this._inprocPorInstances[subGroupName] as string;
                        if (previousInstances != null && previousInstances != string.Empty)
                        {
                            if (previousInstances.IndexOf(nodeName) == -1)
                                previousInstances += "," + nodeName;
                        }
                        else
                        {
                            previousInstances = nodeName;
                        }
                        this._inprocPorInstances[subGroupName] = previousInstances;
                    }
                    else
                    {
                        this._inprocPorInstances.Add(subGroupName, nodeName);
                    }

                    for (int i = 0; i < this._porNodes.Count; i++)
                    {
                        string porNodes = this._porNodes[i] as string;
                        if (porNodes.Trim().IndexOf(subGroupName) == 0)
                        {
                            if (porNodes.IndexOf(nodeName) == -1)
                                porNodes += "," + nodeName;
                        }
                        this._porNodes[i] = porNodes;
                    }
                }
                if (!this._porRunningNodeList.Contains(subGroupName))
                {
                    this._runningNodes.Add(nodeName.Trim());
                    this._porRunningNodeList.Add(subGroupName, nodeName.Trim());
                }
                else
                {
                    string nodelist = ((string)this._porRunningNodeList[subGroupName] != null) ? (string)this._porRunningNodeList[subGroupName].ToString().Replace(subGroupName, "") : string.Empty;
                    if (nodelist.IndexOf(nodeName.Trim()) == -1)
                    {
                        string currentlist = ((string)this._runningNodes[this._runningNodes.IndexOf(subGroupName + nodelist)]) + "," + nodeName.Trim();
                        this._runningNodes[this._runningNodes.IndexOf(subGroupName + nodelist)] = currentlist;
                        this._porRunningNodeList[subGroupName] = currentlist;
                    }
                }

                if (!_doNotPublish && fireEvent)
                {
                    NodeUp node = new NodeUp(this.ClusterName, nodeName.Trim());
                    node.Fire();
                }
            }
            catch { }

        }
        /// <summary>
        /// Called each time a node lefts the cluster
        /// </summary>
        /// <param name="NodeName">Name of the Node</param>
        public void MemberLeft(string port, string nodeName, bool fireEvent)
        {
            //lock (this._syncPoint)
            //{
            try
            {
                if (this._inprocInstances.Contains(nodeName.Trim() + "." + port))
                {
                    nodeName = nodeName.Trim() + "." + port;
                    this._inprocInstances.Remove(nodeName);
                    this._nodes.Remove(nodeName);//remove the inproc instance from nodes too
                }
                if (_runningNodes.IndexOf(nodeName.Trim()) != -1)
                {
                    _runningNodes.Remove(nodeName.Trim());
                    NodeRemoved nodeRemoved = new NodeRemoved(this.ClusterName, nodeName.Trim());

                    /* Commented to stop posting on WMI */
                    if (fireEvent) nodeRemoved.Fire();
                }
            }
            catch { }
            //}
        }

        public void MemberLeft(string port, string nodeName, string subGroupName, bool fireEvent, bool isInproc)
        {
            try
            {
                if (isInproc)
                {
                    nodeName = nodeName.Trim() + "." + port;
                    string previousInstances = this._inprocPorInstances[subGroupName] as string;
                    if (previousInstances != null && previousInstances != string.Empty && previousInstances.Trim() != nodeName)
                    {
                        previousInstances = previousInstances.Replace(nodeName, "").Replace(",,", ",").TrimEnd(',');
                    }
                    else
                    {
                        this._inprocPorInstances.Remove(subGroupName);
                    }
                    for (int i = 0; i < this._porNodes.Count; i++)
                    {
                        string porInstances = this._porNodes[i] as string;
                        if (porInstances.IndexOf(subGroupName) != -1)
                        {
                            porInstances = porInstances.Replace(nodeName, "").Replace(",,", ",").TrimEnd(',');
                            this._porNodes[i] = porInstances;
                            break;
                        }
                    }
                }
                if (this._porRunningNodeList.Contains(subGroupName))
                {
                    string nodeRemoved = (string)this._porRunningNodeList[subGroupName];
                    if (nodeRemoved.IndexOf(nodeName.Trim() + ",") != -1)
                    {
                        this._runningNodes[this._runningNodes.IndexOf(nodeRemoved)] = nodeRemoved.Replace(nodeName + ",", "");
                        this._porRunningNodeList[subGroupName] = nodeRemoved.Replace(nodeName + ",", "");
                    }
                    else if (nodeRemoved.IndexOf("," + nodeName.Trim()) != -1)
                    {
                        this._runningNodes[this._runningNodes.IndexOf(nodeRemoved)] = nodeRemoved.Replace("," + nodeName.Trim(), "");
                        this._porRunningNodeList[subGroupName] = nodeRemoved.Replace("," + nodeName.Trim(), "");
                    }

                    else if (nodeRemoved.IndexOf(nodeName) != -1)
                    {
                        this._runningNodes[this._runningNodes.IndexOf(nodeRemoved)] = nodeRemoved.Replace(nodeName.Trim(), "");
                        this._porRunningNodeList[subGroupName] = nodeRemoved.Replace(nodeName.Trim(), "");
                    }

                    if (!_doNotPublish && fireEvent)
                    {
                        NodeRemoved nodeRemovedevent = new NodeRemoved(this.ClusterName, nodeName.Trim());
                        nodeRemovedevent.Fire();
                    }
                }
            }
            catch { }
        }
        /// <summary>
        /// Add the nodeName to the List of Nodes
        /// </summary>
        /// <param name="nodeName"></param>
        public void AddNode(string nodeName)
        {
            try
            {
                if (_nodes.IndexOf(nodeName.Trim()) == -1)
                    _nodes.Add(nodeName.Trim());
            }
            catch { }
        }

        /// <summary>
        /// Add the nodeName to the List of Nodes
        /// </summary>
        /// <param name="nodeName"></param>
        public void AddPorNode(string nodeName)
        {
            try
            {
                if (_porNodes.IndexOf(nodeName.Trim()) == -1)
                    _porNodes.Add(nodeName.Trim());
            }
            catch { }
        }

        /// <summary>
        /// Removes the current object from the WMI repository
        /// </summary>
        public void Dispose()
        {

            /* Commented to stop posting on WMI */
            try { System.Management.Instrumentation.Instrumentation.Revoke(this); }
            catch { }
        }


    }



}
#endif