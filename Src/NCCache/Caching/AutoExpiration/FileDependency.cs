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
using System.IO;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
	/// <summary>
	/// Summary description for FileExpiration.
	/// </summary>
	[Serializable]
	public class FileDependency : DependencyHint
	{
		private bool[] _fileExists;
		private bool[] _isDir;
		private string[] _fileName;
		private DateTime[] _lastWriteTime;
        private long _startAfterTicks;

        public FileDependency()         
        {
            _hintType = ExpirationHintType.FileDependency;
        }
        /// <summary>
		/// Initializes a new instance of the FileExpiration class that monitors a file or directory for changes.
		/// </summary>
		public FileDependency(string fileName) : this(fileName, DateTime.Now)
		{
            _hintType = ExpirationHintType.FileDependency;
        }

		/// <summary>
		/// Initializes a new instance of the FileExpiration class that monitors an array of file paths (to files or directories) for changes.
		/// </summary>
		public FileDependency(string[] fileName) : this(fileName, DateTime.Now)
		{
            _hintType = ExpirationHintType.FileDependency;
        }

		/// <summary>
		/// Initializes a new instance of the FileExpiration class that monitors a file or 
		/// directory for changes and indicates when change tracking is to begin.
		/// </summary>
		public FileDependency(string fileName, DateTime startAfter):this(new string[] { fileName }, startAfter)
		{
            _hintType = ExpirationHintType.FileDependency;
        }

		/// <summary>
		/// Initializes a new instance of the FileExpiration class that monitors an array of file
		/// paths (to files or directories) for changes and specifies a time when 
		/// change monitoring begins.
		/// </summary>
		public FileDependency(string[] fileName, DateTime startAfter):base(startAfter)
		{
            _hintType = ExpirationHintType.FileDependency;
            _fileName = fileName;
            Initialize(fileName);
            _startAfterTicks = startAfter.Ticks;
		}



        /// <summary>
        /// Reset dependency settings
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal override bool Reset(CacheRuntimeContext context)
        {
            base.Reset(context);
            return Initialize(this.fileNames);
        }

      
        /// <summary> Returns true if the hint is indexable in expiration manager, otherwise returns false.</summary>
        public override bool IsIndexable { get { return true; } }        

		/// <summary>
		/// returns true when file has changed, returns false otherwise.
		/// </summary>
		public override bool HasChanged { get { return fileExpired(); } }


        /// <summary>
        /// Get the array of file names
        /// </summary>
        public string[] fileNames
        {
            get { return _fileName; }
        }

        /// <summary>
        /// Get ticks for time when change tracking begins
        /// </summary>
        public long StartAfterTicks
        {
            get { return this._startAfterTicks; }
        }

        public override string ToString()
        {
            string toString = "FILEDEPENDENCY \"";
            for (int i = 0; i < _fileName.Length; i++)
                toString += _fileName[i] + "\"";
            toString += "STARTAFTER\"" + this._startAfterTicks + "\"\r\n";
            return toString;
        }
		/// <summary>
		/// Initialize the expiration
		/// </summary>
		/// <param name="fileName"></param>
		private bool Initialize(string[] fileName)
		{
			int length = fileName.Length;
			
			_fileExists = new bool[length];
			_isDir = new bool[length];
			_lastWriteTime = new DateTime[length];
			_fileName = new string[length];

            try
            {
                for (int i = 0; i < length; i++)
                {
                    FileInfo fileInfo = new FileInfo(fileName[i]);
                    _fileName[i] = fileName[i];
                    _isDir[i] = ((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory);
                    if (_isDir[i])
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(fileName[i]);
                        _fileExists[i] = dirInfo.Exists;
                        _lastWriteTime[i] = dirInfo.LastWriteTime;

                    }
                    else
                    {
                        _fileExists[i] = fileInfo.Exists;
                        _lastWriteTime[i] = fileInfo.LastWriteTime;
                    }
                }
            }

            catch (IOException exp) { throw exp; }
            catch (Exception) { throw; }

            return true;
		}

		/// <summary>
		/// If any of the files has modified
		/// </summary>
		/// <returns></returns>
		private bool fileExpired()
		{
			int length = _fileName.Length;
			bool isDir = false;
			bool fileExists = false;
			DateTime lastWriteTime;

			for (int i = 0; i < length; i++)
			{
				// Check if file is removed (we have to keep track if the file 
				// was available at the time of creation of dependency)
				
				FileInfo fileInfo = new FileInfo(_fileName[i]);
				isDir =  (fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
				if (isDir)
				{
					DirectoryInfo dirInfo = new DirectoryInfo(_fileName[i]);
					fileExists = dirInfo.Exists;
					lastWriteTime = dirInfo.LastWriteTime;					
				}
				else
				{
					fileExists = fileInfo.Exists;
					lastWriteTime = fileInfo.LastWriteTime;					
				}

				if (_fileExists[i] == true)
				{
					if (fileExists == false)
					{
						return true;
					}
					else
					{
						// Check if the file is replaced with directory or vice versa
						if (isDir != _isDir[i])
						{
							return true;
						}

						// Check if file is updated
						if (lastWriteTime != _lastWriteTime[i])
						{
							return true;
						}
					}
				}
				else
				{
					if (fileExists == true) return true;
				}
			}
			return false;
		}

        #region ISizable Members

        public override int Size
        {
            get { return base.Size + FileDependencySize; }
        }

        public override int InMemorySize
        {
            get
            {
                int inMemorySize = this.Size;

                inMemorySize += inMemorySize <= 24 ? 0 : Common.MemoryUtil.NetOverHead;

                return inMemorySize;
            }
        }

        private int FileDependencySize
        {
            get
            {                             
                int temp = 0;               
                if(_fileExists != null)
                    temp+=_fileExists.Length+Common.MemoryUtil.NetOverHead;

                if (_isDir != null)
                    temp += _isDir.Length + Common.MemoryUtil.NetOverHead;

                if (_fileName != null)
                    temp += Common.MemoryUtil.GetStringSize(_fileName) + Common.MemoryUtil.NetOverHead; 

                if (_lastWriteTime != null)
                    temp += (Common.MemoryUtil.NetLongSize * _lastWriteTime.Length) + Common.MemoryUtil.NetOverHead;

                temp += Common.MemoryUtil.NetLongSize; //for _startAfterTicks
                                

                return temp;
            }
        }

        #endregion


		#region	/                 --- ICompactSerializable ---           /

		public override void Deserialize(CompactReader reader)
		{
			base.Deserialize(reader);
            _fileExists = (bool[])reader.ReadObject();
			_isDir = (bool[])reader.ReadObject();
			_fileName = (string[])reader.ReadObject();
			_lastWriteTime = (DateTime[])reader.ReadObject();
            _startAfterTicks = reader.ReadInt64();
		}

		public override void Serialize(CompactWriter writer)
		{
			base.Serialize(writer);
			writer.WriteObject(_fileExists);
            writer.WriteObject(_isDir);
            writer.WriteObject(_fileName);
            writer.WriteObject(_lastWriteTime);
            writer.Write(_startAfterTicks);
		}

		#endregion
	}
}
