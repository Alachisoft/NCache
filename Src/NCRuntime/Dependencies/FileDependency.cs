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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Runtime.Exceptions;


namespace Alachisoft.NCache.Runtime.Dependencies
{
    [Serializable]
    public class FileDependency : CacheDependency
    {
        private string[] _fileNames;
        private long _startAfterTicks;

        /// <summary>
        /// Initializes a new instance of the FileExpiration class that monitors a file or directory for changes.
        /// </summary>
        public FileDependency(string fileName)
            : this(fileName, DateTime.Now)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FileExpiration class that monitors an array of file paths (to files or directories) for changes.
        /// </summary>
        public FileDependency(string[] fileName)
            : this(fileName, DateTime.Now)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FileExpiration class that monitors a file or 
        /// directory for changes and indicates when change tracking is to begin.
        /// </summary>
        public FileDependency(string fileName, DateTime startAfter)
            : this(new string[] { fileName }, startAfter)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FileExpiration class that monitors an array of file
        /// paths (to files or directories) for changes and specifies a time when 
        /// change monitoring begins.
        /// </summary>
        public FileDependency(string[] fileName, DateTime startAfter)
        {
            int invalid = 0;
            foreach (string name in fileName)
                if (String.IsNullOrEmpty(name))
                    invalid++;
            if (invalid == fileName.Length)
                throw new OperationFailedException("One of the dependency file(s) does not exist. ");
            _fileNames = fileName;
            _startAfterTicks = startAfter.Ticks;
        }

        /// <summary>
        /// Get the array of file names
        /// </summary>
        public string[] fileNames
        {
            get { return _fileNames; }
        }

        public long StartAfterTicks
        {
            get { return _startAfterTicks; }
        }

     }
}
