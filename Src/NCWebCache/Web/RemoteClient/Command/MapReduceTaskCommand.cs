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
using Alachisoft.NCache.Web.MapReduce;

namespace Alachisoft.NCache.Web.Command
{
    class MapReduceTaskCommand : CommandBase
    {
        byte[] mapper = null;
        byte[] reducer = null;
        byte[] combiner = null;
        byte[] inputProvider = null;
        string taskId = "";
        short outputOption = 0;
        short callbackId = 0;
        byte[] keyFilter = null;
        string query = "";
        byte[] parameters = null;
        private int _methodOverload;

        public MapReduceTaskCommand(object mapper, object reducer, object combiner,
            object inputProvider, string taskId, MROutputOption option, short callbackId,
            object keyFilter, string query, object parameters, int methodOverload)
        {
            if (mapper is byte[])
                this.mapper = (byte[]) mapper;
            if (reducer is byte[])
                this.reducer = (byte[]) reducer;
            if (combiner is byte[])
                this.combiner = (byte[]) combiner;

            if (inputProvider is byte[])
                this.inputProvider = (byte[]) inputProvider;

            if (keyFilter is byte[])
                this.keyFilter = (byte[]) keyFilter;
            if (parameters is byte[])
                this.parameters = (byte[]) parameters;

            this.query = query;
            this.taskId = taskId;
            this.callbackId = callbackId;
            _methodOverload = methodOverload;
            if (option == MROutputOption.IN_MEMORY)
                this.outputOption = 0;
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        internal override CommandType CommandType
        {
            get { return Alachisoft.NCache.Web.Command.CommandType.MAP_REDUCE_TASK; }
        }

        protected override void CreateCommand()
        {
            try
            {
                Common.Protobuf.MapReduceTaskCommand mapReduceCommand = new Common.Protobuf.MapReduceTaskCommand();
                mapReduceCommand.taskId = this.taskId;
                mapReduceCommand.mapper = this.mapper;
                mapReduceCommand.reducer = this.reducer;
                mapReduceCommand.combiner = this.combiner;
                mapReduceCommand.inputProvider = this.inputProvider;
                mapReduceCommand.callbackId = this.callbackId;
                mapReduceCommand.outputOption = this.outputOption;
                mapReduceCommand.keyFilter = this.keyFilter;
                mapReduceCommand.query = this.query;
                mapReduceCommand.queryParameters = this.parameters;

                base._command = new Common.Protobuf.Command();
                base._command.requestID = this.RequestId;
                base._command.mapReduceTaskCommand = mapReduceCommand;
                base._command.type = Common.Protobuf.Command.Type.MAP_REDUCE_TASK;
                base._command.MethodOverload = _methodOverload;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}