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

using Alachisoft.NCache.Storage.Mmf;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Storage
{
	/// <summary>
	/// Implements the RAM based cache storage option. Also implements ICacheStore interface. 
	/// </summary>
	class InMemoryStorageProvider : MmfStorageProvider
	{
		/// <summary> The default size of the memory block to use. </summary>
		protected const UInt32 DEFAULT_SIZE = 16;

		/// <summary>
		/// Overloaded constructor. Takes the properties as a map.
		/// </summary>
		/// <param name="properties">properties collection</param>
		public InMemoryStorageProvider(IDictionary properties,bool evictionEnabled)
		{
			Initialize(properties,evictionEnabled);
		}

		#region /                  Initialize Members                  /

		/// <summary>
		/// Initializes the view manager.
		/// </summary>
		/// <param name="properties">Properties to be set</param>
        public new void Initialize(IDictionary properties, bool evictionEnabled)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");
			try
			{
				properties.Remove("file-name");
				properties["num-views"] = 1;

				uint sizeInMB = DEFAULT_SIZE;
				if (properties.Contains("max-size"))
					sizeInMB = Convert.ToUInt32(properties["max-size"]);

				properties["view-size"] = sizeInMB * StorageProviderBase.MB;
				properties["initial-size-mb"] = sizeInMB;
				base.Initialize(properties,evictionEnabled);
			}
			catch (Exception)
			{
				throw;
			}
		}

        #endregion


    }
}
