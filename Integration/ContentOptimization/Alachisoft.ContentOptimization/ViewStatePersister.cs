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
using System;
using System.Web.UI;
using Alachisoft.ContentOptimization.Caching;
using Alachisoft.ContentOptimization.Diagnostics.Logging;

namespace Alachisoft.ContentOptimization
{
    public class ViewStatePersister : PageStatePersister
    {
        const string CacheKeyPrefix = "vs:";

        ICache cache;
        ITraceProvider trace;
        HiddenFieldPageStatePersister _hiddenFiledPagePersister;
        
        public ViewStatePersister(Page page, ICache cache, ITraceProvider trace): base(page)
        {
            this.cache = cache;
            this.trace = trace;
            _hiddenFiledPagePersister = new HiddenFieldPageStatePersister(page);
        }

        public override void Load()
        {
             _hiddenFiledPagePersister.Load();

            string viewStateStr = _hiddenFiledPagePersister.ViewState as string; // Page.Request.Form[STATE_KEY];
            trace.WriteTrace(TraceSeverity.InformationEvent, TraceCategory.Content, "View state: "+ viewStateStr);

            if (viewStateStr != null && viewStateStr.StartsWith(CacheKeyPrefix)) // state not inline check the cached state key
            {
                    string state = LoadState(viewStateStr);
                    if (state == null)
                        trace.WriteTrace(TraceSeverity.CriticalEvent, TraceCategory.Content, "View state lost. Probably cached item expired.");
                    else
                    {
                        try
                        {
                            Pair data = (Pair)StateFormatter.Deserialize(state);
                            ViewState = data.First;
                            ControlState = data.Second;
                        }
                        catch
                        {
                            trace.WriteTrace(TraceSeverity.Exception, TraceCategory.Content, String.Format("Could not deserialize view state. View state is invalid."));
                        }
                    }
            }
            else
            {
                ViewState = _hiddenFiledPagePersister.ViewState;
                ControlState = _hiddenFiledPagePersister.ControlState;
            }   
        }

        public override void Save()
        {
            if(cache.Loaded)
            {
                string cacheKey = CacheKeyPrefix + Guid.NewGuid().ToString("N"); // dynamic guid 
                Pair data = new Pair(ViewState, ControlState);
                string state = StateFormatter.Serialize(data);
                
                
                bool cached = false;
                try
                {
                        cached = cache.Insert(cacheKey, state); //insert without grouping
                }
                catch (Exception e)
                {
                    if (!cached)
                        trace.WriteTrace(TraceSeverity.WarningEvent, TraceCategory.Content, "Could not cache view state."+e.Message);
                }

                if (cached)
                {
                    _hiddenFiledPagePersister.ViewState = cacheKey; // send dynamic guid to client
                    ContentPerfCounters.Current.UpdateViewstateSize(state.Length);
                    ContentPerfCounters.Current.IncrementViewstateAdditions();
                    trace.WriteTrace(TraceSeverity.InformationEvent, TraceCategory.Content, "view state cached: " + cacheKey);
                }
            }
            else
            {
                trace.WriteTrace(TraceSeverity.CriticalEvent, TraceCategory.Content, " Cache is not loaded. ");

                _hiddenFiledPagePersister.ViewState = ViewState;
                _hiddenFiledPagePersister.ControlState = ControlState;
            }

            _hiddenFiledPagePersister.Save();            
        }

        private string LoadState(string cacheKey)
        {
            string state = null;
            if (cacheKey != null)
            {
                state = cache.Get(cacheKey) as string;
                bool cacheHit = state != null;

                ContentPerfCounters.Current.IncrementViewstateRequests();

                if (cacheHit)
                    ContentPerfCounters.Current.IncrementViewstateCacheHits();
                else
                    ContentPerfCounters.Current.IncrementViewstateCacheMisses();
            }

            return state;
        }
    }
}
