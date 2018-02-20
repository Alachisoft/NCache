using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Caching;
using System;
using System.Collections;
using System.Reflection;

namespace Alachisoft.NCache.Caching.DatasourceProviders
{
    /// <summary>
    /// Base class for datasource specific tasks. 
    /// </summary>
    class CacheResyncTask : AsyncProcessor.IAsyncTask
    {
        /// <summary> The parent on this task. </summary>
        private DatasourceMgr _parent;
        /// <summary> Key for the item. </summary>
        private string _key;
        /// <summary> item. </summary>
        private object _val;
        /// <summary> item. </summary>
        private ExpirationHint _exh;
        /// <summary> item. </summary>
        private EvictionHint _evh;
        /// <summary></summary>
        private BitSet _flag;
        /// <summary></summary>

        /// <summary></summary>
        private GroupInfo _groupInfo;
        /// <summary></summary>
        private Hashtable _queryInfo;

        private string _resyncProviderName;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        public CacheResyncTask(DatasourceMgr parent, string key, ExpirationHint exh, EvictionHint evh, GroupInfo groupInfo, Hashtable queryInfo, string resyncProviderName)
        {
            _parent = parent;
            _key = key;
            _exh = exh;
            _evh = evh;
            _groupInfo = groupInfo;
            _queryInfo = queryInfo;
            _resyncProviderName = resyncProviderName;
        }

        public object Value { get { return _val; } }
        public ExpirationHint ExpirationHint { get { return _exh; } }
        public EvictionHint EvictionHint { get { return _evh; } }
        public BitSet Flag { get { return this._flag; } }
        public GroupInfo GroupInfo { get { return this._groupInfo; } }
        public Hashtable QueryInfo { get { return this._queryInfo; } }

        /// <summary> Do write-thru now. </summary>
        public void Process()
        {
            lock (this)
            {
                try
                {
                    if (_val == null)
                    {
                        ProviderCacheItem item = null;
                        LanguageContext languageContext = LanguageContext.NONE;
                        OperationContext operationContext = new OperationContext();
                        CacheEntry entry = null;
                        object userBrinaryObject = null;
                        try
                        {
                            _parent.ReadThru(_key, out item, _resyncProviderName, out languageContext, operationContext);
                            userBrinaryObject = _parent.GetCacheEntry(_key, item, ref this._flag, _groupInfo != null ? _groupInfo.Group : null, _groupInfo != null ? _groupInfo.SubGroup : null, out entry, languageContext);
                        }
                        catch (Exception ex)
                        {
                            _val = ex;
                            _parent.Context.NCacheLog.Error("DatasourceMgr.ResyncCacheItem", ex.Message + " " + ex.StackTrace);
                        }
                        if (!(_val is Exception) && userBrinaryObject != null)
                        {
                            operationContext.Add(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                            CacheInsResultWithEntry result = _parent.Context.CacheImpl.Insert(_key, entry, true, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                            if (result != null && result.Result == CacheInsResult.IncompatibleGroup) _parent.Context.CacheImpl.Remove(_key, ItemRemoveReason.Removed, true, null, 0, LockAccessType.IGNORE_LOCK, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                        }
                        else
                        {
                            _parent.Context.CacheImpl.Remove(_key, ItemRemoveReason.Removed, true, null, 0, LockAccessType.IGNORE_LOCK, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                        }
                    }
                }
                catch (Exception e)
                {
                    _val = e;
                    _parent.Context.NCacheLog.Error("DatasourceMgr.ResyncCacheItem", e.Message + " " + e.StackTrace);
                }
                finally
                {
                    _parent.Context.PerfStatsColl.IncrementCountStats(_parent.Context.CacheInternal.Count);
                    _parent.Queue.Remove(_key);
                }
            }
        }

        private Hashtable GetQueryInfo(Object value)
        {
            Hashtable queryInfo = null;

            if (_parent.Context.CacheImpl.TypeInfoMap == null)
                return null;

            try
            {
                string typeName = value.GetType().FullName;
                typeName = typeName.Replace("+", ".");

                int handleId = _parent.Context.CacheImpl.TypeInfoMap.GetHandleId(typeName);
                if (handleId != -1)
                {
                    queryInfo = new Hashtable();
                    Type valType = null;
                    ArrayList attribValues = new ArrayList();
                    ArrayList attributes = _parent.Context.CacheImpl.TypeInfoMap.GetAttribList(handleId);

                    for (int i = 0; i < attributes.Count; i++)
                    {
                        PropertyInfo propertyAttrib = value.GetType().GetProperty((string)attributes[i]);
                        if (propertyAttrib != null)
                        {
                            Object attribValue = propertyAttrib.GetValue(value, null);

                            if (attribValue is System.String) //add all strings as lower case in index tree
                            {
                                attribValue = (object)(attribValue.ToString()).ToLower();
                            }
                            attribValues.Add(attribValue);
                        }
                        else
                        {
                            FieldInfo fieldAttrib = value.GetType().GetField((string)attributes[i]);
                            if (fieldAttrib != null)
                            {
                                Object attribValue = fieldAttrib.GetValue(value);

                                if (attribValue is System.String) //add all strings as lower case in index tree
                                {
                                    attribValue = (object)(attribValue.ToString()).ToLower();
                                }
                                attribValues.Add(attribValue);
                            }
                            else
                            {
                                throw new Exception("Unable extracting query information from user object.");
                            }
                        }
                    }
                    queryInfo.Add(handleId, attribValues);
                }
            }
            catch (Exception) { }
            return queryInfo;
        }

    }
}
