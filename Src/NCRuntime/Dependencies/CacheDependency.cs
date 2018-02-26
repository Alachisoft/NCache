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
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.Runtime.Dependencies
{
	/// <summary>
	/// Tracks cache dependencies, which can be files, directories, or keys to other objects in 
	/// application's Cache. This class cannot be inherited.
	/// </summary>
    /// <remarks>
    ///
    /// This Feature is Not Available in Express
    ///
	/// You can add items with dependencies to your application's cache with the 
	/// <see cref="Cache.Add"/> and Insert methods.
	/// <para>When you add an item to an application's <see cref="Cache"/> object and in doing so 
	/// define a cache dependency for that item, an instance of the 
	/// <see cref="CacheDependency"/> class is created automatically to track changes to the files, 
	/// keys, or directories you have specified. This helps you avoid 
	/// losing changes made to the object between the time it is created and the 
	/// time it is inserted into the <see cref="Cache"/>. The <see cref="CacheDependency"/> instance can 
	/// represent a single file or directory, an array of files or 
	/// directories, or an array of files or directories along with an array of 
	/// cache keys (these represent other items stored in the <see cref="Cache"/> object).
	/// </para>
	/// </remarks>
	[Serializable]
	public class CacheDependency : IDisposable
	{
        private DateTime _startAfter;
        private List<CacheDependency> _dependencies;

		#region	/                 --- Constructors ---           /

		public CacheDependency()
		{
            _dependencies = new List<CacheDependency>();
		}

		/// <summary>
		/// Creates a CacheDependency instance from extensible dependency.
		/// </summary>
		/// <param name="hint">ExtensibleDependency hint.</param>
		public CacheDependency (ExtensibleDependency extensibleDependency)
		{
            if (_dependencies == null)
                _dependencies = new List<CacheDependency>();

            _dependencies.Add(extensibleDependency);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CacheDependency"/> class that monitors a file 
		/// or directory for changes.
		/// </summary>
		/// <param name="fileName">The path to a file or directory that the cached object is dependent 
		/// upon. When this resource changes, the cached object becomes obsolete and is removed from the 
		/// cache.</param>
		/// <remarks>
		/// If the directory or file specified in the <paramref name="fileName"/> parameter is not found in 
		/// the file system, it will be treated as a missing file. If the file is created after the object 
		/// with the dependency is added to the <see cref="Cache"/>, the cached object will be removed from the 
		/// <see cref="Cache"/>.
		/// <para>
		/// For example, assume that you add an object to the <see cref="Cache"/> with a dependency on the following 
		/// file path: c:\stocks\xyz.dat. If that file is not found when the <see cref="CacheDependency"/> object is 
		/// created, but is created later, the cached object is removed upon the creation of the xyz.dat file.</para>
		/// </remarks>
		/// <example>The following example demonstrates code that creates an instance of the <see cref="CacheDependency"/> class 
		/// when an item is inserted in the <see cref="Cache"/> with a dependency on an XML file.
		/// <code>
		/// 
		///	// Make key1 dependent on a file.
		/// CacheDependency dependency = new CacheDependency(Server.MapPath("isbn.xml"));
        /// Cache cache = NCache.InitializeCache("myCache");
		///	cache.Insert("key1", "Value 1", dependency);
		///
		/// </code>
		/// </example>
		public CacheDependency(string fileName) : this (fileName, DateTime.Now)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CacheDependency"/> class that monitors a 
		/// file or directory for changes and indicates when change tracking is to begin.
		/// </summary>
		/// <param name="fileName">The path to a file or directory that the cached object is dependent 
		/// upon. When this resource changes, the cached object becomes obsolete and is removed from the 
		/// cache.</param>
		/// <param name="start">The time when change tracking begins.</param>
		/// <remarks>
		/// If the directory or file specified in the <paramref name="fileName"/> parameter is not found in 
		/// the file system, it will be treated as a missing file. If the file is created after the object 
		/// with the dependency is added to the <see cref="Cache"/>, the cached object will be removed from the 
		/// <see cref="Cache"/>.
		/// <para>
		/// For example, assume that you add an object to the <see cref="Cache"/> with a dependency on the following 
		/// file path: c:\stocks\xyz.dat. If that file is not found when the <see cref="CacheDependency"/> object is 
		/// created, but is created later, the cached object is removed upon the creation of the xyz.dat file.</para>
		/// </remarks>
		/// <example>The following example demonstrates code that creates an instance of the <see cref="CacheDependency"/> class 
		/// when an item is inserted in the <see cref="Cache"/> with a dependency on an XML file. The tracking start time 
		/// is set to 10 minutes in the future.
		/// <code>
		/// 
		///	// Make key1 dependent on a file.
		/// CacheDependency dependency = new CacheDependency(Server.MapPath("isbn.xml"), DateTime.Now.AddMinutes(10));
        /// Cache cache = NCache.InitializeCache("myCache");
		///	cache.Insert("key1", "Value 1", dependency);
		///
		/// </code>
		/// </example>
		public CacheDependency(string fileName, DateTime start)
		{
            if (fileName == null) throw new ArgumentNullException("fileName");
            if (fileName == string.Empty) throw new ArgumentException("fileName cannot be an empty string");

			if (fileName != null)
			{
                CacheDependency dependency = new FileDependency(fileName, start);
                
                if (_dependencies == null)
                    _dependencies = new List<CacheDependency>();

                _dependencies.Add(dependency);
			}

            _startAfter = start;
		}

		/// <summary>
		/// Initializes a new instance of the CacheDependency class that monitors an 
		/// array of file paths (to files or directories) for changes.
		/// </summary>
		/// <param name="fileNames">An array of file paths (to files or directories) that the cached object 
		/// is dependent upon. When any of these resources change, the cached object becomes obsolete and 
		/// is removed from the cache.</param>
		/// <exception cref="ArgumentNullException"><paramref name="fileNames"/> contains a 
		/// null reference (Nothing in Visual Basic).</exception>
		/// <remarks>
		/// If any of the files or directories in the array were to change or be removed from the array, 
		/// the cached item becomes obsolete and is removed from the application's <see cref="Cache"/> object.
		/// <para>
		/// Also, if any of the directories or files specified in the <paramref name="fileNames"/> parameter is not found in the
		/// file system, they are treated as missing files. If any of them are created after the object with the 
		/// dependency is added to the <see cref="Cache"/>, the cached object will be removed from the <see cref="Cache"/>.For example, 
		/// assume that you add an object to the <see cref="Cache"/> with a dependency on the following file path: 
		/// c:\stocks\xyz.dat. If that file is not found when the <see cref="CacheDependency"/> object is created, but is 
		/// created later, the cached object is removed upon the creation of the xyz.dat file.</para>
		/// </remarks>
		public CacheDependency(string[] fileNames) : this(fileNames, null, null, DateTime.Now)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CacheDependency"/> class that monitors an array of 
		/// file paths (to files or directories) for changes and specifies a time when change 
		/// monitoring begins.
		/// </summary>
		/// <param name="fileNames">An array of file paths (to files or directories) that the cached object 
		/// is dependent upon. When any of these resources change, the cached object becomes obsolete and 
		/// is removed from the cache.</param>
		/// <param name="start">The time when change tracking begins.</param>
		/// <exception cref="ArgumentNullException"><paramref name="fileNames"/> contains a 
		/// null reference (Nothing in Visual Basic).</exception>
		/// <remarks>
		/// If any of the files or directories in the array were to change or be removed from the array, 
		/// the cached item becomes obsolete and is removed from the application's <see cref="Cache"/> object.
		/// <para>
		/// Also, if any of the directories or files specified in the <paramref name="fileNames"/> parameter is not found in the
		/// file system, they are treated as missing files. If any of them are created after the object with the 
		/// dependency is added to the <see cref="Cache"/>, the cached object will be removed from the <see cref="Cache"/>.For example, 
		/// assume that you add an object to the <see cref="Cache"/> with a dependency on the following file path: 
		/// c:\stocks\xyz.dat. If that file is not found when the <see cref="CacheDependency"/> object is created, but is 
		/// created later, the cached object is removed upon the creation of the xyz.dat file.</para>
		/// </remarks>
		public CacheDependency(string[] fileNames, DateTime start) : this(fileNames, null, null, start)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheDependency"/> class that monitors an array of 
        /// file paths (to files or directories), an array of cache keys, or both for changes.
        /// </summary>
        /// <param name="fileNames">An array of file paths (to files or directories) that the cached object 
        /// is dependent upon. When any of these resources change, the cached object becomes obsolete and 
        /// is removed from the cache.</param>
        /// <param name="cacheKeys">An array of cache keys that the new object monitors for changes. When 
        /// any of these cache keys change, the cached object associated with this dependency object 
        /// becomes obsolete and is removed from the cache.</param>
        /// <remarks>
        /// If any of the files or directories in the array were to change or be removed from the array, 
        /// the cached item becomes obsolete and is removed from the application's <see cref="Cache"/> object.
        /// <para>
        /// Also, if any of the directories or files specified in the <paramref name="fileNames"/> parameter is not found in the
        /// file system, they are treated as missing files. If any of them are created after the object with the 
        /// dependency is added to the <see cref="Cache"/>, the cached object will be removed from the <see cref="Cache"/>.For example, 
        /// assume that you add an object to the <see cref="Cache"/> with a dependency on the following file path: 
        /// c:\stocks\xyz.dat. If that file is not found when the <see cref="CacheDependency"/> object is created, but is 
        /// created later, the cached object is removed upon the creation of the xyz.dat file.
        /// </para>
        /// <exception cref="ArgumentNullException"><paramref name="fileNames"/> or <paramref name="cacheKeys"/> contains a 
        /// null reference (Nothing in Visual Basic).</exception>
        /// </remarks>
        /// <example>The following code fragment demonstrates how to insert an item into your application's 
        /// <see cref="Cache"/> with a dependency on a key to another item placed in the cache. Since this method uses 
        /// array syntax, you must define the number of keys the item you are adding to the <see cref="Cache"/> is 
        /// dependent on.</example>

        public CacheDependency(string[] fileNames, string[] cacheKeys)

            : this(fileNames, cacheKeys, null, DateTime.Now)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheDependency"/> class that monitors an array of 
        /// file paths (to files or directories), an array of cache keys, or both for changes.
        /// </summary>
        /// <param name="fileNames">An array of file paths (to files or directories) that the cached object 
        /// is dependent upon. When any of these resources change, the cached object becomes obsolete and 
        /// is removed from the cache.</param>
        /// <param name="cacheKeys">An array of cache keys that the new object monitors for changes. When 
        /// any of these cache keys change, the cached object associated with this dependency object 
        /// becomes obsolete and is removed from the cache.</param>
        /// <param name="start">The time when change tracking begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileNames"/> or <paramref name="cacheKeys"/> contains a 
        /// null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// If any of the files or directories in the array were to change or be removed from the array, 
        /// the cached item becomes obsolete and is removed from the application's <see cref="Cache"/> object.
        /// <para>
        /// Also, if any of the directories or files specified in the <paramref name="fileNames"/> parameter is not found in the
        /// file system, they are treated as missing files. If any of them are created after the object with the 
        /// dependency is added to the <see cref="Cache"/>, the cached object will be removed from the <see cref="Cache"/>.For example, 
        /// assume that you add an object to the <see cref="Cache"/> with a dependency on the following file path: 
        /// c:\stocks\xyz.dat. If that file is not found when the <see cref="CacheDependency"/> object is created, but is 
        /// created later, the cached object is removed upon the creation of the xyz.dat file.
        /// </para>
        /// </remarks>
     
        public CacheDependency(string[] fileNames, string[] cacheKeys, DateTime start)
            : this(fileNames, cacheKeys, null, start)
        {
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="CacheDependency"/> class that monitors an array of 
		/// file paths (to files or directories), an array of cache keys, or both for changes. 
		/// It also makes itself dependent upon a separate instance of the <see cref="CacheDependency"/> class.
		/// </summary>
		/// <param name="fileNames">An array of file paths (to files or directories) that the cached object 
		/// is dependent upon. When any of these resources change, the cached object becomes obsolete and 
		/// is removed from the cache.</param>
		/// <param name="cacheKeys">An array of cache keys that the new object monitors for changes. When 
		/// any of these cache keys change, the cached object associated with this dependency object 
		/// becomes obsolete and is removed from the cache.</param>
		/// <param name="dependency">Another instance of the <see cref="CacheDependency"/> class that this 
		/// instance is dependent upon.</param>
		/// <exception cref="ArgumentNullException"><paramref name="fileNames"/> or <paramref name="cacheKeys"/> contains a 
		/// null reference (Nothing in Visual Basic).</exception>
		/// <remarks>
		/// If any of the files or directories in the array were to change or be removed from the array, 
		/// the cached item becomes obsolete and is removed from the application's <see cref="Cache"/> object.
		/// <para>
		/// Also, if any of the directories or files specified in the <paramref name="fileNames"/> parameter is not found in the
		/// file system, they are treated as missing files. If any of them are created after the object with the 
		/// dependency is added to the <see cref="Cache"/>, the cached object will be removed from the <see cref="Cache"/>.For example, 
		/// assume that you add an object to the <see cref="Cache"/> with a dependency on the following file path: 
		/// c:\stocks\xyz.dat. If that file is not found when the <see cref="CacheDependency"/> object is created, but is 
		/// created later, the cached object is removed upon the creation of the xyz.dat file.
		/// </para>
		/// </remarks>

        public CacheDependency(string[] fileNames, string[] cacheKeys, CacheDependency dependency)
        : this (fileNames, cacheKeys, dependency, DateTime.Now)
		{
		}        	

        /// <summary>
		/// Initializes a new instance of the <see cref="CacheDependency"/> class that monitors an array of file paths (to files or 
		/// directories), an array of cache keys, or both for changes. It also 
		/// makes itself dependent upon another instance of the <see cref="CacheDependency"/> 
		/// class and a time when the change monitoring begins.
		/// </summary>
		/// <param name="fileNames">An array of file paths (to files or directories) that the cached object 
		/// is dependent upon. When any of these resources change, the cached object becomes obsolete and 
		/// is removed from the cache.</param>
		/// <param name="cacheKeys">An array of cache keys that the new object monitors for changes. When 
		/// any of these cache keys change, the cached object associated with this dependency object 
		/// becomes obsolete and is removed from the cache.</param>
		/// <param name="dependency">Another instance of the <see cref="CacheDependency"/> class that this 
		/// instance is dependent upon.</param>
		/// <param name="start">The time when change tracking begins.</param>
		/// <exception cref="ArgumentNullException"><paramref name="fileNames"/> or <paramref name="cacheKeys"/> contains a 
		/// null reference (Nothing in Visual Basic).</exception>
		/// <remarks>
		/// If any of the files or directories in the array were to change or be removed from the array, 
		/// the cached item becomes obsolete and is removed from the application's <see cref="Cache"/> object.
		/// <para>
		/// Also, if any of the directories or files specified in the <paramref name="fileNames"/> parameter is not found in the
		/// file system, they are treated as missing files. If any of them are created after the object with the 
		/// dependency is added to the <see cref="Cache"/>, the cached object will be removed from the <see cref="Cache"/>.For example, 
		/// assume that you add an object to the <see cref="Cache"/> with a dependency on the following file path: 
		/// c:\stocks\xyz.dat. If that file is not found when the <see cref="CacheDependency"/> object is created, but is 
		/// created later, the cached object is removed upon the creation of the xyz.dat file.
		/// </para>
		/// </remarks>


        public CacheDependency(string[] fileNames, string[] cacheKeys, CacheDependency dependency, DateTime start)//:base(start)
		{
            CacheDependency fileDependency = null;
            CacheDependency keyDependency = null;

			if (fileNames != null)
			{
                if (fileNames.Length == 0) throw new ArgumentException("fileNames array must have atleast one file name");
				foreach(string fileName in fileNames)
				{
                    if (fileName == null) throw new ArgumentNullException("fileName");
                    if (fileName == string.Empty) throw new ArgumentException("fileName cannot be empty string");
				}

				fileDependency = new FileDependency(fileNames, start);
			}

            if (cacheKeys != null)
			{
                if (cacheKeys.Length == 0) throw new ArgumentException("fileNames array must have atleast one file name");
				
                foreach(string cachekey in cacheKeys)
				{
                    if (cachekey == null) throw new ArgumentNullException("cacheKey");
                    if (cachekey == string.Empty) throw new ArgumentException("cacheKey cannot be empty string");
				}
				
                keyDependency = new KeyDependency(cacheKeys, start);
			}

            if (fileDependency != null || keyDependency != null || dependency != null)
            {
                if (_dependencies == null)
                    _dependencies = new List<CacheDependency>();

                if (fileDependency != null)
                    _dependencies.Add(fileDependency);

                if (keyDependency != null)
                    _dependencies.Add(keyDependency);

                if (dependency != null)
                    _dependencies.Add(dependency);
            }

            _startAfter = start;
		}

        protected void AddDependencies(params CacheDependency[] dependencies)
        {
            if (_dependencies == null)
                _dependencies = new List<CacheDependency>();

            _dependencies.AddRange(dependencies);
        }


        public List<CacheDependency> Dependencies
        {
            get { return _dependencies; }
        }

		#endregion		

		#region	/                 --- IDisposable ---           /

		/// <summary>
		/// Releases the resources used by the <see cref="CacheDependency"/> object.
		/// </summary>
		/// <remarks>Releases the resources used by the <see cref="CacheDependency"/> object. 
		/// </remarks>
		public void Dispose()
		{
			DependencyDispose();
		}

		/// <summary>
		/// Release custom resources used by the <see cref="CacheDependency"/> object. 
		/// </summary>
		/// <remarks>Releases the resources used by the <see cref="CacheDependency"/> object. Must be 
		/// overriden by custom <see cref="CacheDependency"/> objects.
		/// </remarks>
		protected virtual void DependencyDispose()
		{
		}

		#endregion
	}
}