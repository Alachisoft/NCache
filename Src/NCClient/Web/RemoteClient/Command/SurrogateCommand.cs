using Alachisoft.NCache.Common.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Client
{
    class SurrogateCommand : CommandBase
    {
        private Common.Protobuf.SurrogateCommand _surrogateCommand;

        public CommandBase WrappedCommand { get; private set; }
        public Address ActualTargetNode { get; private set; }

        public SurrogateCommand(CommandBase command,Address actualNode)
        {
            _surrogateCommand = new Common.Protobuf.SurrogateCommand();
            this.WrappedCommand = command;
            this.ActualTargetNode = actualNode;
        }

        internal override RequestType CommandRequestType
        {
            get
            {
                return WrappedCommand.CommandRequestType;
            }
        }


        internal override CommandType CommandType
        {
            get
            {
                return WrappedCommand.CommandType;
            }
        }

        protected override void CreateCommand()
        {
            
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = WrappedCommand.RequestId;
            base._command.type = Common.Protobuf.Command.Type.SURROGATE;
            base._command.version = "4200";


            _surrogateCommand.command.Add(WrappedCommand.GetSerializedSurrogateCommand());
            _surrogateCommand.targetServer = ActualTargetNode.ToString();

            base._command.surrogateCommand = _surrogateCommand;

        }
    }
}
