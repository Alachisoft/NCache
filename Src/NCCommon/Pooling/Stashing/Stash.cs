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
using ProtobufCommand = Alachisoft.NCache.Common.Protobuf.Command;
using ProtobufAddResponse = Alachisoft.NCache.Common.Protobuf.AddResponse;
using ProtobufGetResponse = Alachisoft.NCache.Common.Protobuf.GetResponse;
using ProtobufInsertResponse = Alachisoft.NCache.Common.Protobuf.InsertResponse;
using ProtobufDeleteResponse = Alachisoft.NCache.Common.Protobuf.DeleteResponse;
using ProtobufRemoveResponse = Alachisoft.NCache.Common.Protobuf.RemoveResponse;

namespace Alachisoft.NCache.Common
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
    }
}
