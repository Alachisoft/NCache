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
// limitations under the License

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;

namespace Alachisoft.NCache.Runtime.Dependencies
{
	/// <summary>
	/// Combines multiple dependencies between an item stored in an application's Cache object 
	/// and an array of CacheDependency objects. This class cannot be inherited. 
	/// </summary>
	/// <remarks>
  	/// The AggregateCacheDependency class monitors a collection of dependency objects so that
	/// when any of them changes, the cached item is automatically removed. 
	/// The objects in the array can be <see cref="CacheDependency"/> objects, <see cref="DBCacheDependency"/> objects
	/// or any combination of these. 
	/// <para>
	/// The AggregateCacheDependency class differs from the CacheDependency class in that 
	/// it allows you to associate multiple dependencies of different types with a single 
	/// cached item. For example, if you create a page that imports data from a SQL Server database 
	/// table and an XML file, you can create a SqlCacheDependency object to represent a dependency 
	/// on the database table and a CacheDependency to represent the dependency on the XML file. 
	/// Rather than making an Cache.Insert method call for each dependency, 
	/// you can create an instance of the AggregateCacheDependency class with each 
	/// dependency added to it. You can then use a single Insert call to make the page 
	/// dependent on the AggregateCacheDependency instance.
	/// </para>
	/// </remarks>
	/// <requirements>
	/// <constraint>This member is not available in SessionState edition.</constraint> 
	/// </requirements>
	[Serializable]
	public sealed class AggregateCacheDependency : CacheDependency
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AggregateCacheDependency"/> class that wraps multiple
		/// <see cref="CacheDependency"/> objects.
		/// </summary>
		/// <remarks>
        /// This is the default constructor for the AggregateCacheDependency class.
        /// </remarks>
        public AggregateCacheDependency()
        {
        }   
        /// <summary>
		/// Adds an array of <see cref="CacheDependency"/> objects to the <see cref="AggregateCacheDependency"/> object. 
		/// </summary>
		/// <param name="dependencies">The array of CacheDependency objects to add.</param>
		/// <requirements>
		/// <constraint>This member is not available in SessionState edition.</constraint> 
		/// </requirements>
		public void Add(params CacheDependency[] dependencies)
		{
			if (dependencies == null)
				throw new ArgumentNullException("dependencies");

            AddDependencies(dependencies);
		}
	}
}