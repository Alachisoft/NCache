using Alachisoft.NCache.SocketServer.Statistics;

namespace Alachisoft.NCache.SocketServer
{
    internal class CommandProcessorPool
    {
        private readonly int _maxProcessors;
        private readonly CommandProcessor[] _workers;

        public CommandProcessorPool(int processors, IRequestProcessor reqProcessor, PerfStatsCollector collector)
        {
            _maxProcessors = processors;
            _workers = new CommandProcessor[processors];

            for (int i = 0; i < processors; i++){
                _workers[i] = new CommandProcessor(reqProcessor, collector);
            }
        }

        public void EnqueuRequest(ProcCommand request, uint indexFeed)
        {
            _workers[indexFeed % _maxProcessors].EnqueuRequest(request);
        }

        public void Start()
        {
            lock (this){
                for (int i = 0; i < _maxProcessors; i++){
                    _workers[i].Start();
                }
            }
        }

        public void Stop()
        {
            lock (this){
                for (int i = 0; i < _maxProcessors; i++){
                    _workers[i].Stop();
                }
            }
        }
    }
}
