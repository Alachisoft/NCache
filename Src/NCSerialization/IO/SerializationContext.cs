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
using System.Collections;
using System.Runtime.Serialization;
using Alachisoft.NCache.Common;
namespace Alachisoft.NCache.IO
{
    /// <summary>
    /// A class that maintains serialization/deserialization context. This is used to 
    /// resolve circular and shared references.
    /// </summary>
    public class SerializationContext
    {
        internal const int INVALID_COOKIE = -1;

        /*
         * ISSUE: Using an ArrayList to maintain context doubles the prformance but for 
         * small number of objects only. This is good if there arent any containers, but as
         * the number of objects grow a hashtable performs much better.
         * 
         * TODO: Come up with a structure that performs neraly linearly in most situations.
         */
        /// <summary> Represents a list of objects known in the context so far. </summary>
        private Hashtable graphList = new Hashtable();
        private Hashtable cookieList = new Hashtable();
		private string	  cacheContext;
        private MemoryManager _memManager;
        /// <summary>
        /// Returns cookie for a given graph.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public int GetCookie(object graph)
        {
            if (cookieList.ContainsKey(graph))
            {
                object cookie = cookieList[graph];
                if (cookie != null)
                    return (int)cookie;
            }
            return INVALID_COOKIE;
        }

		/// <summary>
		/// Gets/sets the cache context.
		/// </summary>
		public string CacheContext
		{
			get { return cacheContext; }
			set { cacheContext = value; }
		}

        /// <summary>
        /// Gets or sets the Object Manager.
        /// </summary>
        public MemoryManager MemManager
        {
            get { return _memManager; }
            set { _memManager = value; }
        }

        public SerializationBinder Binder { get; internal set; }

        /// <summary>
        /// Returns a graph by its cookie. If there is no such cookie null is returned.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetObject(int key)
        {
            if (key > SerializationContext.INVALID_COOKIE && key < graphList.Count)
                return graphList[key];
            return null;
        }

        /// <summary>
        /// Adds a graph to the context, assigns a cookie to it. 
        /// Currently the index of the graph is its cookie.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns>the cookie for the object</returns>
        public int RememberObject(object graph, bool updateCookieList)
        {
            int cookie = graphList.Count;
            graphList.Add(cookie, graph);
            //huma: BigCluster fix: We will add in cookieList in serialization flow only.
            //In case of deserilization, we have zero object which may cause exception while insertion in hastable.
            if(updateCookieList) 
                cookieList.Add(graph, cookie);
            return cookie;
        }
    }
}
