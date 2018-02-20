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

using System.Collections.Generic;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Web.Command
{
    class InvokeEntryProcessorCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.InvokeEntryProcessorCommand _invokeEntryProcessorCommand;
        private int _methodOverload;

        public InvokeEntryProcessorCommand(string[] keys, byte[] ep, List<byte[]> arguments, BitSet readOptionFlag,
            string defaultReadThru, BitSet writeOptionFlag, string defaultWriteThru, int methodOverload)
        {
            _invokeEntryProcessorCommand = new Common.Protobuf.InvokeEntryProcessorCommand();
            _invokeEntryProcessorCommand.keys.AddRange(keys);
            _invokeEntryProcessorCommand.entryprocessor = ep;
            if (arguments != null)
                _invokeEntryProcessorCommand.arguments.AddRange(arguments);
            else
                _invokeEntryProcessorCommand.arguments.AddRange(new List<byte[]>());
            _invokeEntryProcessorCommand.dsReadOption = readOptionFlag.Data;
            _invokeEntryProcessorCommand.defaultReadThru = defaultReadThru;
            _invokeEntryProcessorCommand.dsWriteOption = writeOptionFlag.Data;
            _invokeEntryProcessorCommand.defaultWriteThru = defaultWriteThru;
            _methodOverload = methodOverload;
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.KeyBulkRead; }
        }

        internal override CommandType CommandType
        {
            get { return CommandType.INVOKE_ENTRY_PROCESSOR; }
        }

        protected override void CreateCommand()
        {
            base._command = new Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.invokeEntryProcessorCommand = _invokeEntryProcessorCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.INVOKE_ENTRY_PROCESSOR;
            base._command.MethodOverload = _methodOverload;
        }
    }
}