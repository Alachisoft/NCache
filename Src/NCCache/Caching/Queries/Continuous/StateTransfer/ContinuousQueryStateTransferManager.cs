// Copyright (c) 2018 Alachisoft
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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Caching.Topologies.Clustered;
using Alachisoft.NCache.Common.Net;
using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Queries.Continuous.StateTransfer
{
    internal class ContinuousQueryStateTransferManager
    {
        private ClusterCacheBase _parent;
        private ActiveQueryAnalyzer _queryAnalyzer;

        internal ContinuousQueryStateTransferManager(ClusterCacheBase parent, ActiveQueryAnalyzer queryAnalyzer)
        {
            _parent = parent;
            _queryAnalyzer = queryAnalyzer;
        }

        internal void TransferState(Address source)
        {
            try
            {
                ContinuousQueryStateInfo stateInfo = _parent.GetContinuousQueryStateInfo(source);
                if (stateInfo != null)
                {
                    
                    _parent.CQManager.SetState(stateInfo.CQManagerState);

                    if (stateInfo.IsPartial)
                    {
                        ClusteredList<string> registeredTypes = stateInfo.RegisteredTypes;

                        foreach (string type in registeredTypes)
                        {
                            IList<PredicateHolder> predicates = _parent.GetContinuousQueryRegisteredPredicates(type);

                            foreach (PredicateHolder predicate in predicates)
                                predicate.Initialize(_parent.NCacheLog);

                            lock (_queryAnalyzer.TypeSpecificRegisteredPredicates)
                            {
                                _queryAnalyzer.TypeSpecificRegisteredPredicates[type] = predicates;
                            }
                        }
                    }
                    else
                    {
                        foreach (IList<PredicateHolder> predicates in stateInfo.TypeSpecificRegisteredPredicates.Values)
                        {
                            foreach (PredicateHolder predicate in predicates)
                                predicate.Initialize(_parent.NCacheLog);
                        }
                        foreach (object predicates in stateInfo.TypeSpecificPredicates.Values)
                        {
                            IDictionary dict = (IDictionary)predicates;
                            foreach (object predicateHolder in dict.Values)
                            {
                                IList list = (IList)predicateHolder;
                                foreach (object predicate in list)
                                {
                                    PredicateHolder holder = (PredicateHolder)predicate;
                                    holder.Initialize(_parent.NCacheLog);
                                }
                            }
                        }

                        lock (_queryAnalyzer.TypeSpecificRegisteredPredicates)
                        {
                            _queryAnalyzer.TypeSpecificRegisteredPredicates = stateInfo.TypeSpecificRegisteredPredicates;
                        }
                        _queryAnalyzer.TypeSpecificPredicates = stateInfo.TypeSpecificPredicates;
                        _queryAnalyzer.TypeSpecificEvalIndexes = stateInfo.TypeSpecificEvalIndexes;
                    }
                   
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
