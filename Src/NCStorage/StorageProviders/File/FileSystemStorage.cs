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
using System.IO;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Serialization.Formatters;

namespace Alachisoft.NCache.Storage.Util
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	internal class FileSystemStorage: IDisposable
	{
		// The default extension to use with object data files
		private const string		DEFAULT_EXTENSION = ".ncfso";

		/// <summary> Path of the root directory </summary>
		private string				_rootDir;
		/// <summary> Path of the root directory </summary>
		private string _dataFolder;
        
		/// <summary>
		/// Default constructor.
		/// Constructs the file based store using a temporary folder as root directory. 
		/// </summary>
		public FileSystemStorage():this(null, null)
		{
		}

		/// <summary>
		/// Constructs the file based store using the specified folder as the root directory. 
		/// </summary>
		public FileSystemStorage(string rootDir, string dataFolder)
		{
			try
			{
				if(rootDir == null || rootDir.Length < 1)
					rootDir= Path.GetTempPath();
				
				_rootDir = rootDir;
				_dataFolder = dataFolder;

				if (_dataFolder == null)
				{
                    _rootDir = Path.Combine(_rootDir, GetRandomFileName());
                }
				else
				{
					if(Path.IsPathRooted(_dataFolder))
						_dataFolder = Path.GetDirectoryName(_dataFolder);
                    _rootDir = Path.Combine(_rootDir, _dataFolder);
                }


				Directory.CreateDirectory(_rootDir);
				_rootDir += Path.DirectorySeparatorChar;
			}
			catch(Exception err)
			{
				
				throw err;
			}
		}

		#region	/                 --- IDisposable ---           /

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or 
		/// resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			try
			{
                if (_dataFolder == null)
                {
                    if (Directory.Exists(_rootDir))
                    {
                        Directory.Delete(_rootDir, true);
                    }
                }
			}
			catch (Exception err)
			{
			
				throw err;
			}
		}

		#endregion

		/// <summary> 
		/// Path of the root directory 
		/// </summary>
		public string RootDir
		{
			get { return _rootDir; }
		}

		public string DataFolder
		{
			get { return _dataFolder; }
		}


        public string GetRandomFileName()
        {
            return Path.GetRandomFileName() + ".ncdir";
        }

        /// <summary>
		/// Generates a unique filename based upon the specified key.
		/// </summary>
		private string GetUniqueNameForFile(object key)
		{
			Guid guid = Guid.NewGuid();
			string fileName = guid.ToString();
			return fileName;
		}

		/// <summary>
		/// Generates a unique filename based upon the specified key.
		/// </summary>
		private string GetPathForFile(string fileName)
		{
			fileName = String.Concat(_rootDir, fileName, DEFAULT_EXTENSION);
			return fileName;
		}

		/// <summary>
		/// Open a File and deserialize the object from it.
		/// </summary>
        private object ReadObjectFromFile(string fileName, string serializationContext)
		{
			fileName = GetPathForFile(fileName);
			if (!File.Exists(fileName)) 
				return null;

			using (FileStream stream = new FileStream(fileName, FileMode.Open))
			{
                object Value = CompactBinaryFormatter.Deserialize(stream, serializationContext);
				stream.Close();
				return Value;
			}
		}

		/// <summary>
		/// Create a File and serialize the object in to.
		/// </summary>
		private void WriteObjectToFile(string fileName, object value,string serializationContext) 
		{
			fileName = GetPathForFile(fileName);
			using (FileStream stream = new FileStream(fileName, FileMode.Create))
			{
                CompactBinaryFormatter.Serialize(stream, value, serializationContext);
				stream.Close();				
			}
		}

		#region /                   -- Storage Members --                    /

		/// <summary>
		/// Removes all entries from the store.
		/// </summary>		
		public void Clear()
		{
			try
			{
				if (Directory.Exists(_rootDir))
				{
					Directory.Delete(_rootDir, true);
					Directory.CreateDirectory(_rootDir);
				}
			}
			catch (Exception err)
			{
				//Trace.error("FileSystemStorage.Clear()", err.Message.ToString());
				throw err;
			}
		}

		/// <summary>
		/// Get an object from the store, specified by the passed in key. 
		/// </summary>
		/// <param name="Key"></param>
		/// <returns></returns>
		public bool Contains(object key)
		{
			if (key == null) return false;
			try
			{
				return File.Exists(key.ToString());
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>
		/// Get an object from the store, specified by the passed in key. 
		/// </summary>
		/// <param name="Key"></param>
		/// <returns></returns>
		public object Get(object Key,string serializationContext)
		{
			if (Key == null) return null;
			try
			{
                return ReadObjectFromFile(Key.ToString(), serializationContext);
			}
			catch (Exception Err)
			{
				//Trace.error("FileSystemStorage.Get()", Err.Message.ToString());
				throw Err;
			}
		}
	
		/// <summary>
		/// Add Value in FileSystemStorage
		/// </summary>
		/// <param name="Value"></param>
		/// <returns></returns>
		public string Add(object key, object Value,string serializationContext)
		{
			try
			{
				string fileName = GetUniqueNameForFile(key);
                this.WriteObjectToFile(fileName, Value, serializationContext);
				return fileName;
			}
			catch(Exception err)
			{
				//Trace.error("FileSystemStorage.Add()",err.Message.ToString());
				throw err;
			}
		}
		
		/// <summary>
		/// Add key value Pair in FileSystemStorage
		/// </summary>
		/// <param name="Key"></param>
		/// <param name="Value"></param>
		public string Insert(object key, object value,string serializationContext)
		{
			try
			{
				string fileName = null;
				if (key == null)
				{
                    fileName = Add(key, value, serializationContext);
				}
				else 
				{
					fileName = key.ToString();
                    this.WriteObjectToFile(fileName, value, serializationContext);
				}
				return fileName;				
			}
			catch(Exception err)
			{
				//Trace.error("FileSystemStorage.Insert()",err.Message.ToString());
				throw err;
			}
		}
		
		/// <summary>
		/// Removes an object from the store, specified by the passed in key
		/// </summary>
		/// <param name="Key"></param>
		public void Remove(object Key)
		{
			try
			{
				string fileName = GetPathForFile(Key.ToString());
				if (File.Exists(fileName)) File.Delete(fileName);
			}
			catch(Exception err)
			{
				//Trace.error("FileSystemStorage.Remove()",err.Message.ToString());
				throw err;
			}
		}

		#endregion
	}
}
