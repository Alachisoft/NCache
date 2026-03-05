using Alachisoft.NCache.Common.Stats;

namespace Alachisoft.NCache.SocketServer
{
    public interface ICommand
    {
        object Command { get; set; }
        short CommandType { get; set; }
        long AcknowledgementId { get; set; }
        ClientManager ClientManager { get; set; }
        UsageStats Stats { get; set; }
    }
}
