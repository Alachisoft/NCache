using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Client
{
    class ModuleCommand : CommandBase
    {
        Alachisoft.NCache.Common.Protobuf.ModuleCommand _moduleCommand = new Common.Protobuf.ModuleCommand();

        public ModuleCommand(byte[] value,string module,string version)
        {
            _moduleCommand.payload.Add(value);
            _moduleCommand.version = version;
            _moduleCommand.module = module;
        }

        public override bool SupportsSurrogation
        {
            get
            {
                return true;
            }
        }

        internal override RequestType CommandRequestType
        {
            get
            {
                return RequestType.KeyBulkWrite;
            }
        }

        internal override CommandType CommandType
        {
            get
            {
               return CommandType.MODULE;
            }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.moduleCommand = _moduleCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.MODULE;
            base._command.version = "4200";
        }
    }
}
