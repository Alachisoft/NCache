// Copyright (c) 2017 Alachisoft
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Integrations.Memcached.Provider;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.Commands
{
    class StorageCommand : AbstractCommand
    {
        private string _key;
        private uint _flags;
        private long _expirationTimeInSeconds;
        private object _dataBlock;
        private int _dataSize;
        private ulong _casUnique;

        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public uint Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        public long ExpirationTimeInSeconds
        {
            get { return _expirationTimeInSeconds; }
            set { _expirationTimeInSeconds = value; }
        }

        public ulong CASUnique
        {
            get { return _casUnique; }
            set { _casUnique = value; }
        }

        public object DataBlock
        {
            get { return _dataBlock; }
            set { _dataBlock = value; }
        }

        public int DataSize
        {
            get { return _dataSize; }
            set { _dataSize = value; }
        }

        public StorageCommand(Opcode cmdType, string key, uint flags, long expirationTimeInSeconds, int dataSize)
            : base(cmdType)
        {
            _key = key;
            _flags = flags;
            _expirationTimeInSeconds = expirationTimeInSeconds;
            _dataSize = dataSize;
        }

        public StorageCommand(Opcode cmdType, string key, uint flags, long expirationTimeInSeconds, ulong casUnique, int dataSize)
            : base(cmdType)
        {
            _key = key;
            _flags = flags;
            _expirationTimeInSeconds = expirationTimeInSeconds;
            _dataSize = dataSize;
            _casUnique = casUnique;
        }

        public override void Execute(IMemcachedProvider cacheProvider)
        {
            if (this._errorMessage != null)
                return;

            if (_casUnique > 0)
                _result = cacheProvider.CheckAndSet(_key, _flags, _expirationTimeInSeconds, _casUnique, _dataBlock);
            else
                switch (this.Opcode)
                {
                    case Opcode.Set:
                    case Opcode.SetQ:
                        _result = cacheProvider.Set(_key, _flags, _expirationTimeInSeconds, _dataBlock);
                        break;
                    case Opcode.Add:
                    case Opcode.AddQ:
                        _result = cacheProvider.Add(_key, _flags, _expirationTimeInSeconds, _dataBlock);
                        break;
                    case Opcode.Replace:
                    case Opcode.ReplaceQ:
                        _result = cacheProvider.Replace(_key, _flags, _expirationTimeInSeconds, _dataBlock);
                        break;
                    case Opcode.Append:
                    case Opcode.AppendQ:
                        _result = cacheProvider.Append(_key, (byte[])_dataBlock, _casUnique);
                        break;
                    case Opcode.Prepend:
                    case Opcode.PrependQ:
                        _result = cacheProvider.Prepend(_key, (byte[])_dataBlock, _casUnique);
                        break;
                    case Opcode.CAS:
                        _result = cacheProvider.CheckAndSet(_key, _flags, _expirationTimeInSeconds, _casUnique, _dataBlock);
                        break;
                }
        }
    }
}
