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
namespace Alachisoft.NCache.Caching.Topologies.History
{
#if DEBUGSTATETRANSFER

    internal class Activity 
    {
        protected string _toString;

        internal protected Activity(string activity)
        {
            this._toString = activity;
        }

        public override string ToString()
        {
            return _toString;
        }
    }

    internal class ViewActivity : Activity
    {
        View _view;
        ArrayList _leftMmbrs;
        ArrayList _joinMmbrs;

        public ViewActivity(View view, ArrayList leftMmbrs, ArrayList joinMmbrs) : base(null)
        {
            this._view = view;
            this._leftMmbrs = leftMmbrs;
            this._joinMmbrs = joinMmbrs;
            this._toString = this.ToString();
        }

        public override string ToString()
        {
            return "{" + _view.ToString() + "} -(" + Global.CollectionToString(_leftMmbrs) + ") +(" + Global.CollectionToString(_joinMmbrs) + ")";
        }
    }

    internal class CorresponderActivity : Activity
    {
        int _transferType;
        Address _requestingNode;
        int  _bucketId;
        int _expectedTxferId;

        public CorresponderActivity(int transferType, Address requestingNode, int bucketId, int expectedTrxferId) : base(null)
        {
            this._transferType = transferType;
            this._requestingNode = requestingNode;
            this._bucketId = bucketId;
            this._expectedTxferId = expectedTrxferId;
            this._toString = this.ToString();
        }

        public override string ToString()
        {
            return (Enum.Parse(typeof(StateTransferType), _transferType.ToString())).ToString() + " | " + _requestingNode.ToString() + " | " + _bucketId + " | " + _expectedTxferId;
        }
    }

    internal class StateTxferActivity : Activity
    {
        int _bucketId;
        Address _owner;
        int _expectedTxfrId;

        public StateTxferActivity(int bucketId, Address owner, int expectedTxfrId)
            : base(null)
        {
            this._bucketId = bucketId;
            this._owner = owner;
            this._expectedTxfrId = expectedTxfrId;
            this._toString = this.ToString();
        }

        public override string ToString()
        {
            return _bucketId + " | " + _owner + " | " + _expectedTxfrId;
        }
    }

    internal class StateTxferUpdateActivity : Activity
    {
        ArrayList _filledBuckets;
        ArrayList _sparsedBuckets;
        bool _replica;

        public StateTxferUpdateActivity(ArrayList filledBuckets, ArrayList sparsedBuckets)
            : base(null)
        {
            this._filledBuckets = filledBuckets;
            this._sparsedBuckets = sparsedBuckets;
            this._toString = this.ToString();
        }

        public override string ToString()
        {
            return "(" + Global.CollectionToString(_filledBuckets) + ")" + Environment.NewLine
                + "(" + Global.CollectionToString(_sparsedBuckets)  + ")";
        }
    }

    internal class NodeActivities
    {
        Hashtable _activities = Hashtable.Synchronized(new Hashtable());

        public void AddActivity(Activity activity)
        {
            _activities.Add(DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss.fff tt"), activity);
        }

    }

   

#endif
}
