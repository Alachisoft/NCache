// Copyright (c) 2015 Alachisoft
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
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Commands;
using Alachisoft.NCache.Integrations.Memcached.Provider;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway;
using System.Collections;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.BinaryProtocol
{
    class BinaryResponseBuilder
    {
        private static byte[] BuildResposne(Opcode opcode, BinaryResponseStatus status, int opaque, ulong cas, string key, byte[] value, byte[] extra)
        {
            BinaryResponse response = new BinaryResponse();
            response.Header.Opcode = opcode;
            response.Header.Status = status;
            response.Header.Opaque = opaque;
            response.Header.CAS = cas;
            response.PayLoad.Key = MemcachedEncoding.BinaryConverter.GetBytes(key);
            response.PayLoad.Value = value;
            response.PayLoad.Extra = extra;
            return response.BuildResponse();
        }

        public static byte[] BuildQuitResponse(QuitCommand command)
        {
            if (command.NoReply == true)
                return null;

            return BuildResposne(command.Opcode, BinaryResponseStatus.no_error, command.Opaque, 0, null, null, null);
        }

        public static byte[] BuildInvalidResponse(InvalidCommand command)
        {
            BinaryResponseStatus status;
            if (command!=null && command.Opcode == Opcode.unknown_command)
                status = BinaryResponseStatus.unknown_commnad;
            else
                status = BinaryResponseStatus.invalid_arguments;

            return BuildResposne(command.Opcode, status, command.Opaque, 0, null, null, null);
        }

        public static byte[] BuildVersionResponse(VersionCommand command)
        {
            byte[] value = null;

            if (command.OperationResult.ReturnResult == Result.SUCCESS)
            {
                string version = command.OperationResult.Value as string;
                value = MemcachedEncoding.BinaryConverter.GetBytes(version);
            }
            return BuildResposne(command.Opcode, BinaryResponseStatus.no_error, command.Opaque, 0, null, value, null);
        }

        public static byte[] BuildCounterResponse(CounterCommand command)
        {
            byte[] value = null;
            BinaryResponseStatus status = BinaryResponseStatus.no_error;
            ulong cas = 0;

            if (command.ExceptionOccured)
                status = BinaryResponseStatus.item_not_stored;
            else
            {
                switch (command.OperationResult.ReturnResult)
                {
                    case Result.ITEM_TYPE_MISMATCHED:
                        status = BinaryResponseStatus.incr_decr_on_nonnumeric_value;
                        value = MemcachedEncoding.BinaryConverter.GetBytes("Increment or decrement on non-numeric value");
                        break;
                    case Result.ITEM_NOT_FOUND:
                        status = BinaryResponseStatus.key_not_found;
                        value = MemcachedEncoding.BinaryConverter.GetBytes("NOT_FOUND");
                        break;
                    case Result.SUCCESS:
                        if (command.NoReply == true)
                            return null;
                        cas = (ulong)command.OperationResult.Value;
                        ulong response = (command.OperationResult as MutateOpResult).MutateResult;
                        value = MemcachedEncoding.BinaryConverter.GetBytes(response);
                        break;
                    case Result.ITEM_MODIFIED:
                        status = BinaryResponseStatus.key_exists;
                        break;
                }
            }
            return BuildResposne(command.Opcode, status, command.Opaque, cas, null, value, null);
        }

        public static byte[] BuildGetResponse(GetCommand command)
        {
            string key = "";
            byte[] value=new byte[0];
            uint flag=0;
            ulong cas = 0;
            BinaryResponseStatus status = BinaryResponseStatus.no_error;
            if (command.ExceptionOccured)
                status = BinaryResponseStatus.key_not_found;
            else if (command.Results.Count > 0)
            {
                cas = command.Results[0].Version;
                value = command.Results[0].Value as byte[];
                flag = command.Results[0].Flag;

                if (command.Opcode == Opcode.GetK)
                    key = command.Results[0].Key;
            }
            else
            {
                if (command.NoReply == true)
                    return null;
                status = BinaryResponseStatus.key_not_found;
            }

            byte[] flagBytes = MemcachedEncoding.BinaryConverter.GetBytes(flag);
            return BuildResposne(command.Opcode, status, command.Opaque, cas, key, value, flagBytes);
        }

        public static byte[] BuildNoOpResponse(NoOperationCommand command)
        {
            return BuildResposne(command.Opcode, BinaryResponseStatus.no_error, command.Opaque, 0, null, null, null);
        }

        public static byte[] BuildDeleteResponse(DeleteCommand command)
        {
            BinaryResponseStatus status = BinaryResponseStatus.no_error;
            if (command.OperationResult.ReturnResult != Result.SUCCESS)
                status = BinaryResponseStatus.key_not_found;
            else
            {
                if (command.NoReply == true)
                    return null;
            }
            return BuildResposne(command.Opcode, status, command.Opaque, 0, null, null, null);
        }

        public static byte[] BuildStorageResponse(StorageCommand command)
        {
            BinaryResponseStatus status = BinaryResponseStatus.no_error;
            ulong cas = 0;
            if (command.ExceptionOccured)
            {
                status = BinaryResponseStatus.item_not_stored;
            }
            else
            {
                switch (command.OperationResult.ReturnResult)
                {
                    case Result.SUCCESS:
                        if (command.NoReply == true)
                            return null;
                        cas = (ulong)command.OperationResult.Value;
                        break;
                    case Result.ITEM_EXISTS:
                        status = BinaryResponseStatus.key_exists;
                        break;
                    case Result.ITEM_NOT_FOUND:
                        status = BinaryResponseStatus.key_not_found;
                        break;
                    case Result.ITEM_MODIFIED:
                        status = BinaryResponseStatus.key_exists;
                        break;
                    default:
                        status = BinaryResponseStatus.item_not_stored;
                        break;
                }
            }
            return BuildResposne(command.Opcode, status, command.Opaque, cas, null, null, null);
        }

        public static byte[] BuildFlushResponse(FlushCommand command)
        {
            BinaryResponseStatus status = BinaryResponseStatus.no_error;
            if (command.OperationResult.ReturnResult != Result.SUCCESS)
                status = BinaryResponseStatus.invalid_arguments;
            else
            {
                if (command.NoReply == true)
                    return null;
            }
            return BuildResposne(command.Opcode, status, command.Opaque, 0, null, null, null);
        }

        public static byte[] BuildStatsResponse(StatsCommand command)
        {
            DataStream stream = new DataStream();
            Hashtable stats= command.OperationResult.Value as Hashtable;
            IDictionaryEnumerator ie = stats.GetEnumerator();

            string key = "";
            byte [] value = null;
            while (ie.MoveNext())
            {
                key = ie.Key as string;
                value = MemcachedEncoding.BinaryConverter.GetBytes(ie.Value as string);
                stream.Write(BuildResposne(command.Opcode, BinaryResponseStatus.no_error, command.Opaque, 0, key, value, null));
            }
            
            stream.Write(BuildResposne(command.Opcode, BinaryResponseStatus.no_error, command.Opaque, 0, null, null, null));
            return stream.ReadAll();
        }
    }
}
