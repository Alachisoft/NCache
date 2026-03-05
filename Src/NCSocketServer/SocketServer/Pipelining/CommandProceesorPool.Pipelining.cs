namespace Alachisoft.NCache.SocketServer.Pipelining
{
    internal class CommandProcessorPool
    {
        private readonly int _maxProcessors;
        private readonly CommandProcessor[] _workers;

        public CommandProcessorPool(int processors, IRequestProcessor reqProcessor)
        {
            _maxProcessors = processors;
            _workers = new CommandProcessor[processors];

            for (int i = 0; i < processors; i++)
            {
                _workers[i] = new CommandProcessor(reqProcessor);
            }
        }

        public void EnqueuRequest(LongRunningCommand request, long indexFeed)
        {
            _workers[indexFeed % _maxProcessors].EnqueuRequest(request);
        }

        public void Start()
        {
            lock (this)
            {
                for (int i = 0; i < _maxProcessors; i++)
                {
                    _workers[i].Start();
                }
            }
        }

        public void Stop()
        {
            lock (this)
            {
                for (int i = 0; i < _maxProcessors; i++)
                {
                    _workers[i].Stop();
                }
            }
        }
    }
}
