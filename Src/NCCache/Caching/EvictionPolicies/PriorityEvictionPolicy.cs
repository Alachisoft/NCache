// Copyright (c) 2015 Alachisoft
// 
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
using System;
using System.Collections;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.DataStructures.Clustered;


namespace Alachisoft.NCache.Caching.EvictionPolicies
{
    /// <summary>
	/// Priority based Eviction Policy
	/// When needed objects with lowest priority are removed first
	/// </summary>
	internal class PriorityEvictionPolicy : IEvictionPolicy
	{

		/// <summary> default priority </summary>
		private CacheItemPriority _priority = CacheItemPriority.Normal;
        private HashVector[] _index;
        private float _ratio = 0.25F;

        ///It is the interval between two consecutive removal of items from the cluster so that user operation is not affected
        private int _sleepInterval = 0;  //milliseconds

        ///No of items which can be removed in a single clustered operation.
        private int _removeThreshhold = 10;

		/// <summary>
		/// Constructor.
		/// </summary>
		internal PriorityEvictionPolicy()
		{			
			Initialize();
		}

		/// <summary>
		/// Overloaded constructor
		/// Initializes the object based on the properties specified in configuration
		/// and eviction ratio
		/// </summary>
		/// <param name="properties"></param>
		/// <param name="ratio"></param>
		public PriorityEvictionPolicy(IDictionary properties, float ratio) 
		{
			if(properties != null)
			{
				if(properties.Contains("default-value"))
				{
			        string defaultValue = (string) properties["default-value"];
					_priority = GetPriorityValue(defaultValue);
				}
			}
            _sleepInterval = Convert.ToInt32(ServiceConfiguration.EvictionBulkRemoveDelay);
            _removeThreshhold = Convert.ToInt32(ServiceConfiguration.EvictionBulkRemoveSize);
            _ratio = ratio / 100f;
			Initialize();
		}


		/// <summary>
		/// Initialize policy
		/// </summary>
		private void Initialize()
		{
            _index = new HashVector[5];
		}


		/// <summary>
		/// Check if the provided eviction hint is compatible with the policy
		/// and return the compatible eviction hint
		/// </summary>
		/// <param name="eh">eviction hint.</param>
		/// <returns>a hint compatible to the eviction policy.</returns>
		public EvictionHint CompatibleHint(EvictionHint eh)
		{
			if (eh != null && eh is PriorityEvictionHint)
			{
				return eh;
			} 
			return new PriorityEvictionHint(_priority);
		}


		/// <summary>
		/// Convert the string representation of Priority to PriorityVale enumeration
		/// </summary>
		/// <param name="priority"></param>
		/// <returns></returns>
		private static CacheItemPriority GetPriorityValue(string priority)
		{
			priority = priority.ToLower();
			switch(priority)
			{
				case "notremovable": return CacheItemPriority.NotRemovable;
				case "high": return CacheItemPriority.High;
				case "above-normal": return CacheItemPriority.AboveNormal;
				case "below-normal": return CacheItemPriority.BelowNormal;
				case "low": return CacheItemPriority.Low;
			}
			return CacheItemPriority.Normal;
		}

        float IEvictionPolicy.EvictRatio
        {
            get { return _ratio; }
            set { _ratio = value; }
        }

        void IEvictionPolicy.Notify(object key, EvictionHint oldhint, EvictionHint newHint)
        {
            
            //always use the new priority eviction hint.
            EvictionHint hint = newHint;
            if (hint != null)
            {
                CacheItemPriority hintPriority = ((PriorityEvictionHint)hint).Priority;

                if (hintPriority == CacheItemPriority.Default)
                {
                    hintPriority = this._priority;//set the default priority from the config.
                    ((PriorityEvictionHint)hint).Priority = this._priority;
                }

                if ((oldhint != null))
                {

                    CacheItemPriority oldPriority = ((PriorityEvictionHint)oldhint).Priority;
                    CacheItemPriority newPriority = ((PriorityEvictionHint)newHint).Priority;
                    if (oldPriority != newPriority)
                    {
                        IEvictionPolicy temp = this as IEvictionPolicy;
                        temp.Remove(key, oldhint);
                    }


                }

                lock (_index.SyncRoot)
                {
                    int changedIndex = -1;
                    switch (hintPriority)
                    {
                        case CacheItemPriority.Low:
                            if (_index[0] == null)
                                _index[0] = new HashVector();
                            _index[0][key] = hint;
                            changedIndex = 0;
                            break;
						case CacheItemPriority.BelowNormal:
                            if (_index[1] == null)
                                _index[1] = new HashVector();
                            _index[1][key] = hint;
                            changedIndex = 1;
                            break;
						case CacheItemPriority.Normal:
                            if (_index[2] == null)
                                _index[2] = new HashVector();
                            _index[2][key] = hint;
                            changedIndex = 2;
                            break;
						case CacheItemPriority.AboveNormal:
                            if (_index[3] == null)
                                _index[3] = new HashVector();
                            _index[3][key] = hint;
                            changedIndex = 3;
                            break;
						case CacheItemPriority.High:
                            if (_index[4] == null)
                                _index[4] = new HashVector();
                            _index[4][key] = hint;
                            changedIndex = 4;
                            break;
                    }                   
                }
            }
        }

        void IEvictionPolicy.Execute(CacheBase cache, CacheRuntimeContext context, long evictSize)
        {
          
            ILogger NCacheLog = cache.Context.NCacheLog;
            
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("LocalCache.Evict()", "Cache Size: {0}" + cache.Count.ToString());

             //if user has updated the values in configuration file then new values will be reloaded.
            _sleepInterval = Convert.ToInt32(ServiceConfiguration.EvictionBulkRemoveDelay);
            _removeThreshhold = Convert.ToInt32(ServiceConfiguration.EvictionBulkRemoveSize);
            DateTime startTime = DateTime.Now;
            IList selectedKeys = this.GetSelectedKeys(cache, (long)Math.Ceiling(evictSize * _ratio));
            DateTime endTime = DateTime.Now;

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("LocalCache.Evict()", String.Format("Time Span for {0} Items: " + (endTime - startTime), selectedKeys.Count));

            startTime = DateTime.Now;

            Cache rootCache = context.CacheRoot;

            ClusteredArrayList keysTobeRemoved = new ClusteredArrayList();
            ClusteredArrayList dependentItems = new ClusteredArrayList();
            IList removedItems = null;

            IEnumerator e = selectedKeys.GetEnumerator();

            int removedThreshhold = _removeThreshhold/300;
            int remIteration = 0;
            while (e.MoveNext())
            {
                object key = e.Current;
                if (key == null) continue;

                keysTobeRemoved.Add(key);

                if (keysTobeRemoved.Count % 300 == 0)
                {
                    try
                    {
                        OperationContext priorityEvictionOperationContext = new OperationContext();
                        priorityEvictionOperationContext.Add(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                        removedItems = cache.RemoveSync(keysTobeRemoved.ToArray(), ItemRemoveReason.Underused, false, priorityEvictionOperationContext) as ArrayList;

                        context.PerfStatsColl.IncrementEvictPerSecStatsBy(keysTobeRemoved.Count);
                    }
                    catch (Exception ex)
                    {
                        NCacheLog.Error("PriorityEvictionPolicy.Execute", "an error occurred while removing items. Error " + ex.ToString());
                    }
                    keysTobeRemoved.Clear();
                    if (removedItems != null && removedItems.Count > 0)
                    {
                        dependentItems.AddRange(removedItems);
                    }
                    
                    remIteration++;
                    if (remIteration >= removedThreshhold)
                    {
                        //put some delay so that user operations are not affected.
                        System.Threading.Thread.Sleep(_sleepInterval*1000);
                        remIteration = 0;
                    }
                }
            }

            if (keysTobeRemoved.Count > 0)
            {
                try
                {
                    OperationContext priorityEvictionOperationContext = new OperationContext();
                    priorityEvictionOperationContext.Add(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                    removedItems = cache.RemoveSync(keysTobeRemoved.ToArray(), ItemRemoveReason.Underused, false, priorityEvictionOperationContext) as ArrayList;

                    context.PerfStatsColl.IncrementEvictPerSecStatsBy(keysTobeRemoved.Count);
                    if (removedItems != null && removedItems.Count > 0)
                    {
                        dependentItems.AddRange(removedItems);
                    }
                }
                catch (Exception ex)
                {
                    NCacheLog.Error("PriorityEvictionPolicy.Execute", "an error occurred while removing items. Error " + ex.ToString());
                }
            }

            if (dependentItems.Count > 0)
            {
                ArrayList removableList = new ArrayList();
                if (rootCache != null)
                {
                    foreach (object depenentItme in dependentItems)
                    {
                        if (depenentItme == null) continue;
                        removableList.Add(depenentItme);
                        if (removableList.Count % 100 == 0)
                        {
                            try
                            {
                                OperationContext priorityEvictionOperationContext = new OperationContext();
                                priorityEvictionOperationContext.Add(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                                rootCache.CascadedRemove(removableList, ItemRemoveReason.Underused, true, priorityEvictionOperationContext);

                                context.PerfStatsColl.IncrementEvictPerSecStatsBy(removableList.Count);
                            }
                            catch (Exception exc)
                            {
                                NCacheLog.Error("PriorityEvictionPolicy.Execute", "an error occurred while removing dependent items. Error " + exc.ToString());

                            }
                            removableList.Clear();
                        }

                    }
                    if (removableList.Count > 0)
                    {
                        try
                        {
                            OperationContext priorityEvictionOperationContext = new OperationContext();
                            priorityEvictionOperationContext.Add(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                            rootCache.CascadedRemove(removableList, ItemRemoveReason.Underused, true, priorityEvictionOperationContext);

                            context.PerfStatsColl.IncrementEvictPerSecStatsBy(removableList.Count);
                        }
                        catch (Exception exc)
                        {
                            NCacheLog.Error("PriorityEvictionPolicy.Execute", "an error occurred while removing dependent items. Error " + exc.ToString());

                        }
                        removableList.Clear();
                    }
                }
            }
            return;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="evictSize"></param>
        /// <returns></returns>
        private IList GetSelectedKeys(CacheBase cache, long evictSize)
        {
            ClusteredArrayList selectedKeys = new ClusteredArrayList(100);

            long sizeCount = 0;
            int prvsSize = 0;
            object key = null;
            bool selectionComplete = false;

            lock (_index.SyncRoot)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (!selectionComplete)
                    {
                        HashVector currentIndex = this._index[i];
                        if (currentIndex != null)
                        {
                            IDictionaryEnumerator ide = currentIndex.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                key = ide.Key;
                                if (key != null)
                                {
                                    int itemSize = cache.GetItemSize(key);
                                    if (sizeCount + itemSize >= evictSize && sizeCount > 0)
                                    {
                                        if (evictSize - sizeCount > (itemSize + sizeCount) - evictSize)
                                            selectedKeys.Add(key);

                                        selectionComplete = true;
                                        break;
                                    }
                                    else
                                    {
                                        selectedKeys.Add(key);
                                        sizeCount += itemSize;
                                        prvsSize = itemSize;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        //break the outer loop. we have already picked up
                        //the keys to be evicited.
                        break; 
                    }
                }
            }
            return selectedKeys;
        }

        void IEvictionPolicy.Remove(object key, EvictionHint hint)
        {
            lock (_index.SyncRoot)
            {
                if (_index != null)
                    for (int i = 0; i < 5; i++)
                    {
                        if (_index[i] != null)
                            if (_index[i].Contains(key))
                            {
                                _index[i].Remove(key);
                                if (_index[i].Count == 0)
                                {
                                    _index[i] = null;
                                }
                            }
                    }
            }
        }

        void IEvictionPolicy.Clear()
        {
            lock (_index.SyncRoot)
            {
                if (_index != null)
                    for (int i = 0; i < 5; i++)
                        if (_index[i] != null)
                        {
                            _index[i] = new HashVector();
                        }
            }
        }


        #region ISizable Impelementation
        public long IndexInMemorySize { get { return PriorityEvictionIndexSize; } }

        private long PriorityEvictionIndexSize
        {
            get
            {
                int keysCount = 0;
                int evictionIndexMaxCounts = 0;
                long temp = 0;

                if (_index != null)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (_index[i] != null)
                        {
                            keysCount += _index[i].Count;
                            evictionIndexMaxCounts += _index[i].BucketCount;
                        }
                    }
                }
                temp += keysCount * PriorityEvictionHint.InMemorySize;
                temp += evictionIndexMaxCounts * Common.MemoryUtil.NetHashtableOverHead;

                return temp;
            }
        }
        #endregion
    }
}


