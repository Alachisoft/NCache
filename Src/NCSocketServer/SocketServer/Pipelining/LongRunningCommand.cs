using Alachisoft.NCache.Common.Stats;

namespace Alachisoft.NCache.SocketServer.Pipelining
{
    public class LongRunningCommand : ICommand
    {
     
        public object Command { get; set; }
        public short CommandType { get; set; }
        public long AcknowledgementId { get; set; }
        public ClientManager ClientManager { get; set; }
        public UsageStats Stats { get; set; }
    }
}

