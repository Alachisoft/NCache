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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;


#region ------------------------------- [ PROTBUF COMMANDS ] ------------------------------
using ProtobufCommand = Alachisoft.NCache.Common.Protobuf.Command;
using ProtobufAddCommand = Alachisoft.NCache.Common.Protobuf.AddCommand;
using ProtobufGetCommand = Alachisoft.NCache.Common.Protobuf.GetCommand;
using ProtobufInsertCommand = Alachisoft.NCache.Common.Protobuf.InsertCommand;
using ProtobufDeleteCommand = Alachisoft.NCache.Common.Protobuf.DeleteCommand;
using ProtobufRemoveCommand = Alachisoft.NCache.Common.Protobuf.RemoveCommand;
#endregion

#region ------------------------------ [ PROTBUF RESPONSES ] ------------------------------
using ProtobufResponse = Alachisoft.NCache.Common.Protobuf.Response;
using ProtobufAddResponse = Alachisoft.NCache.Common.Protobuf.AddResponse;
using ProtobufGetResponse = Alachisoft.NCache.Common.Protobuf.GetResponse;
using ProtobufInsertResponse = Alachisoft.NCache.Common.Protobuf.InsertResponse;
using ProtobufDeleteResponse = Alachisoft.NCache.Common.Protobuf.DeleteResponse;
using ProtobufRemoveResponse = Alachisoft.NCache.Common.Protobuf.RemoveResponse;
#endregion

#region ---------------------------- [ SOCKET SERVER COMMANDS ] ---------------------------
using SocketServerAddCommand = Alachisoft.NCache.SocketServer.Command.AddCommand;
using SocketServerGetCommand = Alachisoft.NCache.SocketServer.Command.GetCommand;
using SocketServerInsertCommand = Alachisoft.NCache.SocketServer.Command.InsertCommand;
using SocketServerDeleteCommand = Alachisoft.NCache.SocketServer.Command.DeleteCommand;
using SocketServerRemoveCommand = Alachisoft.NCache.SocketServer.Command.RemoveCommand;
#endregion

namespace Alachisoft.NCache.SocketServer
{
    internal static class Stash
    {
        [ThreadStatic]
        private static BitSet _bitSet;

        public static BitSet BitSet
        {
            get
            {
                if (_bitSet == null)
                    _bitSet = new BitSet();

                _bitSet.ResetLeasable();

                return _bitSet;
            }
        }

        [ThreadStatic]
        private static CacheEntry _cacheEntry;

        public static CacheEntry CacheEntry
        {
            get
            {
                if (_cacheEntry == null)
                    _cacheEntry = new CacheEntry();

                _cacheEntry.ResetLeasable();

                return _cacheEntry;
            }
        }

   
      

        #region ------------------------------- [ PROTBUF COMMANDS ] ------------------------------

        [ThreadStatic]
        private static ProtobufCommand _protobufCommand;

        public static ProtobufCommand ProtobufCommand
        {
            get
            {
                if (_protobufCommand == null)
                    _protobufCommand = new ProtobufCommand();

                _protobufCommand.ResetLeasable();

                return _protobufCommand;
            }
        }

        [ThreadStatic]
        private static ProtobufAddCommand _protobufAddCommand;

        public static ProtobufAddCommand ProtobufAddCommand
        {
            get
            {
                if (_protobufAddCommand == null)
                    _protobufAddCommand = new ProtobufAddCommand();

                _protobufAddCommand.ResetLeasable();

                return _protobufAddCommand;
            }
        }

        [ThreadStatic]
        private static ProtobufGetCommand _protobufGetCommand;

        public static ProtobufGetCommand ProtobufGetCommand
        {
            get
            {
                if (_protobufGetCommand == null)
                    _protobufGetCommand = new ProtobufGetCommand();

                _protobufGetCommand.ResetLeasable();

                return _protobufGetCommand;
            }
        }

        [ThreadStatic]
        private static ProtobufInsertCommand _protobufInsertCommand;

        public static ProtobufInsertCommand ProtobufInsertCommand
        {
            get
            {
                if (_protobufInsertCommand == null)
                    _protobufInsertCommand = new ProtobufInsertCommand();

                _protobufInsertCommand.ResetLeasable();

                return _protobufInsertCommand;
            }
        }

        [ThreadStatic]
        private static ProtobufDeleteCommand _protobufDeleteCommand;

        public static ProtobufDeleteCommand ProtobufDeleteCommand
        {
            get
            {
                if (_protobufDeleteCommand == null)
                    _protobufDeleteCommand = new ProtobufDeleteCommand();

                _protobufDeleteCommand.ResetLeasable();

                return _protobufDeleteCommand;
            }
        }

        [ThreadStatic]
        private static ProtobufRemoveCommand _protobufRemoveCommand;

        public static ProtobufRemoveCommand ProtobufRemoveCommand
        {
            get
            {
                if (_protobufRemoveCommand == null)
                    _protobufRemoveCommand = new ProtobufRemoveCommand();

                _protobufRemoveCommand.ResetLeasable();

                return _protobufRemoveCommand;
            }
        }

        #endregion

        #region ------------------------------ [ PROTBUF RESPONSES ] ------------------------------

        [ThreadStatic]
        private static ProtobufResponse _protobufResponse;

        public static ProtobufResponse ProtobufResponse
        {
            get
            {
                if (_protobufResponse == null)
                    _protobufResponse = new ProtobufResponse();

                _protobufResponse.ResetLeasable();

                return _protobufResponse;
            }
        }

        [ThreadStatic]
        private static ProtobufAddResponse _protobufAddResponse;

        public static ProtobufAddResponse ProtobufAddResponse
        {
            get
            {
                if (_protobufAddResponse == null)
                    _protobufAddResponse = new ProtobufAddResponse();

                _protobufAddResponse.ResetLeasable();

                return _protobufAddResponse;
            }
        }

        [ThreadStatic]
        private static ProtobufGetResponse _protobufGetResponse;

        public static ProtobufGetResponse ProtobufGetResponse
        {
            get
            {
                if (_protobufGetResponse == null)
                    _protobufGetResponse = new ProtobufGetResponse();

                _protobufGetResponse.ResetLeasable();

                return _protobufGetResponse;
            }
        }

        [ThreadStatic]
        private static ProtobufInsertResponse _protobufInsertResponse;

        public static ProtobufInsertResponse ProtobufInsertResponse
        {
            get
            {
                if (_protobufInsertResponse == null)
                    _protobufInsertResponse = new ProtobufInsertResponse();

                _protobufInsertResponse.ResetLeasable();

                return _protobufInsertResponse;
            }
        }

        [ThreadStatic]
        private static ProtobufDeleteResponse _protobufDeleteResponse;

        public static ProtobufDeleteResponse ProtobufDeleteResponse
        {
            get
            {
                if (_protobufDeleteResponse == null)
                    _protobufDeleteResponse = new ProtobufDeleteResponse();

                _protobufDeleteResponse.ResetLeasable();

                return _protobufDeleteResponse;
            }
        }

        [ThreadStatic]
        private static ProtobufRemoveResponse _protobufRemoveResponse;

        public static ProtobufRemoveResponse ProtobufRemoveResponse
        {
            get
            {
                if (_protobufRemoveResponse == null)
                    _protobufRemoveResponse = new ProtobufRemoveResponse();

                _protobufRemoveResponse.ResetLeasable();

                return _protobufRemoveResponse;
            }
        }

        #endregion

        #region ---------------------------- [ SOCKET SERVER COMMANDS ] ---------------------------

        [ThreadStatic]
        private static SocketServerAddCommand _socketServerAddCommand;

        public static SocketServerAddCommand SocketServerAddCommand
        {
            get
            {
                if (_socketServerAddCommand == null)
                    _socketServerAddCommand = new SocketServerAddCommand();

                _socketServerAddCommand.ResetLeasable();

                return _socketServerAddCommand;
            }
        }

        [ThreadStatic]
        private static SocketServerGetCommand _socketServerGetCommand;

        public static SocketServerGetCommand SocketServerGetCommand
        {
            get
            {
                if (_socketServerGetCommand == null)
                    _socketServerGetCommand = new SocketServerGetCommand();

                _socketServerGetCommand.ResetLeasable();

                return _socketServerGetCommand;
            }
        }

        [ThreadStatic]
        private static SocketServerInsertCommand _socketServerInsertCommand;

        public static SocketServerInsertCommand SocketServerInsertCommand
        {
            get
            {
                if (_socketServerInsertCommand == null)
                    _socketServerInsertCommand = new SocketServerInsertCommand();

                _socketServerInsertCommand.ResetLeasable();

                return _socketServerInsertCommand;
            }
        }

        [ThreadStatic]
        private static SocketServerDeleteCommand _socketServerDeleteCommand;

        public static SocketServerDeleteCommand SocketServerDeleteCommand
        {
            get
            {
                if (_socketServerDeleteCommand == null)
                    _socketServerDeleteCommand = new SocketServerDeleteCommand();

                _socketServerDeleteCommand.ResetLeasable();

                return _socketServerDeleteCommand;
            }
        }

        [ThreadStatic]
        private static SocketServerRemoveCommand _socketServerRemoveCommand;

        public static SocketServerRemoveCommand SocketServerRemoveCommand
        {
            get
            {
                if (_socketServerRemoveCommand == null)
                    _socketServerRemoveCommand = new SocketServerRemoveCommand();

                _socketServerRemoveCommand.ResetLeasable();

                return _socketServerRemoveCommand;
            }
        }

        #endregion
    }
}
