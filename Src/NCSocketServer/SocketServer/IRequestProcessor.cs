namespace Alachisoft.NCache.SocketServer
{
    interface IRequestProcessor
    {
        void Process(ProcCommand procCommand);
    }
}
