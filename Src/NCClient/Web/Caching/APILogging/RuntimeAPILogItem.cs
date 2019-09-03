//  Copyright (c) 2018 Alachisoft
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

namespace Alachisoft.NCache.Web.Caching.APILogging
{
    class RuntimeAPILogItem
    {
        private bool _isForSentObject = true;
        public bool IsForSentObject
        {
            get { return _isForSentObject; }
            set { _isForSentObject = value; }
        }

        private bool _isBulk = false;
        public bool IsBulk
        {
            get { return _isBulk; }
            set { _isBulk = value; }
        }

        private int _noOfObjects = 1;
        public int NoOfObjects
        {
            get { return _noOfObjects; }
            set { _noOfObjects = value; }
        }

        private long _sizeOfObject = 0;
        public long SizeOfObject
        {
            get { return _sizeOfObject; }
            set { _sizeOfObject = value; }
        }

        private bool _encryptionEnabled = false;
        public bool EncryptionEnabled
        {
            get { return _encryptionEnabled; }
            set { _encryptionEnabled = value; }
        }

        private long _sizeOfEncryptedObject = 0;
        public long SizeOfEncryptedObject
        {
            get { return _sizeOfEncryptedObject; }
            set { _sizeOfEncryptedObject = value; }
        }

        private bool _compressionEnabled = false;
        public bool CompressionEnabled
        {
            get { return _compressionEnabled; }
            set { _compressionEnabled = value; }
        }

        private long _sizeOfCompressedObject = 0;
        public long SizeOfCompressedObject
        {
            get { return _sizeOfCompressedObject; }
            set { _sizeOfCompressedObject = value; }
        }
    }
}
