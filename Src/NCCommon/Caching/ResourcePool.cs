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

namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// Contains the list of open sql connections. Sql7CacheDependency
    /// asks for the connection from the connection pool whenever required. 
    /// The connection is added to the pool at the time of creation of the dependency and
    /// is removed from the connection pool when no dependency object is using it.
    /// For every interim call for a connection from the pool, its referrence count is 
    /// incremented and referrence count is decremented when a dependency object, using it, 
    /// disposes.
    /// </summary>
    public class ResourcePool: IDisposable
	{
		public class ResourceInfo
		{
			private object _resource;
			private int _refCount;

			public ResourceInfo(object resource)
			{
				_resource = resource;
			}

			public int AddRef() { return ++_refCount; }
			public int Release() 
			{ 
				if(_refCount > 0) --_refCount; 
				return _refCount; 
			}

			public object Object { get { return _resource; } set { _resource = value; } }
		}

		private Hashtable resourceTable;

		/// <summary>
		/// Static constructor. Initializes the static _connectionTable.
		/// </summary>
		public ResourcePool()
		{
			resourceTable = new Hashtable( new DelegateComparer());
		}

		#region	/                 --- IDisposable ---           /

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or 
		/// resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			lock(this)
			{
				IDictionaryEnumerator em = resourceTable.GetEnumerator();
				while (em.MoveNext())
				{
					ResourceInfo res = (ResourceInfo)em.Value;
					DisposeResource(res.Object);
				}
			}
		}

		#endregion

		/// <summary>
		/// 
		/// </summary>
		public ICollection Keys
		{
			get
			{
				return resourceTable.Keys;
			}
		}

		public int Count
		{
			get
			{
				return resourceTable.Count;
			}
		}

		/// <summary>
		/// If available, returns the requested connection from the _connectionTable.
		/// Otherwise, returns null.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public object GetResource(object key)
		{
			ResourceInfo resourceInfo = resourceTable[key] as ResourceInfo;
			if (resourceInfo != null)
			{
				return resourceInfo.Object;
			}
			return null;
		}

		/// <summary>
		/// Add the resource to resource pool, and increase its reference count
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public void AddResource(object key, object value)
		{
			ResourceInfo resourceInfo = resourceTable[key] as ResourceInfo;

			if (resourceInfo != null)
			{
				if (value != null) resourceInfo.Object = value;
			}
			else
			{
				resourceInfo = new ResourceInfo(value);
			}

			resourceTable[key] = resourceInfo;
			resourceInfo.AddRef();
		}

        public void AddResource(object key, object value, int numberOfCallbacks)
        {
            ResourceInfo resourceInfo = resourceTable[key] as ResourceInfo;

            if (resourceInfo != null)
            {
                if (value != null) resourceInfo.Object = value;
            }
            else
            {
                resourceInfo = new ResourceInfo(value);
            }

            resourceTable[key] = resourceInfo;

            for (int i = 0; i < numberOfCallbacks; i++)
            {
                resourceInfo.AddRef();
            }
        }


        /// <summary>
        /// If available, returns the requested connection from the _connectionTable.
        /// Otherwise, returns null.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object RemoveResource(object key)
		{
			ResourceInfo resourceInfo = resourceTable[key] as ResourceInfo;
			if(resourceInfo != null)
			{
				if(resourceInfo.Release() == 0)
				{
					resourceTable.Remove(key);
					DisposeResource(resourceInfo.Object);
				}
				return resourceInfo.Object;
			}
			return null;
		}

        public object RemoveResource(object key, int numberOfCallbacks)
        {
            ResourceInfo resourceInfo = resourceTable[key] as ResourceInfo;
            if (resourceInfo != null)
            {
                for (int i = 0; i < numberOfCallbacks; i++)
                {
                    if (resourceInfo.Release() == 0)
                    {
                        resourceTable.Remove(key);
                        DisposeResource(resourceInfo.Object);
                    }
                }
                return resourceInfo.Object;
            }
            return null;
        }

        /// <summary>
        /// Removes the Severed Resource from the pool.
        /// </summary>
        /// <param name="key"></param>
        public void RemoveSeveredResource(object key)
        {
            ResourceInfo resourceInfo = resourceTable[key] as ResourceInfo;
            if (resourceInfo != null)
            {
                resourceTable.Remove(key);
                DisposeResource(resourceInfo.Object);
            }
        }

		/// <summary>
		/// Remove all the resources from resource table
		/// </summary>
		public void RemoveAllResources()
		{
			lock (this)
			{
				ICollection keys = resourceTable.Keys;
				IEnumerator ie = keys.GetEnumerator();

				while(ie.MoveNext())
				{
					ResourceInfo resourceInfo = resourceTable[ie.Current] as ResourceInfo;
					if(resourceInfo != null)
					{
						DisposeResource(resourceInfo.Object);
					}
				}

				resourceTable.Clear();
			}
		}

		/// <summary>
		/// If available, returns the requested connection from the _connectionTable.
		/// Otherwise, returns null.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		private void DisposeResource(object res)
		{
			if(res is IDisposable)
			{
				try
				{
					((IDisposable)res).Dispose();
				}
				catch(Exception e)
				{
					Trace.warn("ResourcePool.Dispose()", e.Message);
				}
			}
		}

        public object[] GetAllResourceKeys()
        {
            lock (this)
            {
                object[] keysCopy = new object[resourceTable.Keys.Count];
                this.resourceTable.Keys.CopyTo(keysCopy, 0);
                return keysCopy;
            }
        }

        public object[] GetAllResourceValues()
        {
            lock (this)
            {
                object[] valueCopy = new object[resourceTable.Values.Count];
                this.resourceTable.Values.CopyTo(valueCopy, 0);
                return valueCopy;
            }
        }
    }
}