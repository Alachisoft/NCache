// SNMP application class.
// Copyright (C) 2009-2010 Lex Li
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// SNMP application class, who is a pipeline for message processing.
    /// </summary>
    public sealed class SnmpApplication
    {
        private readonly ILogger _logger;
        private readonly IMembershipProvider _provider;
        private readonly MessageHandlerFactory _factory;
        private IMessageHandler _handler;
        private readonly ObjectStore _store;
        private readonly SnmpApplicationFactory _owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnmpApplication"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="store">The store.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="factory">The factory.</param>
        public SnmpApplication(SnmpApplicationFactory owner, ILogger logger, ObjectStore store, IMembershipProvider provider, MessageHandlerFactory factory)
        {
            _owner = owner;
            _provider = provider;
            _logger = logger;
            _store = store;
            _factory = factory;
        }

        /// <summary>
        /// Inits the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Init(ISnmpContext context)
        {
            Context = context;
            ProcessingFinished = false;
            _handler = null;
        }

        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>The context.</value>
        public ISnmpContext Context { get; private set; }

        /// <summary>
        /// Gets a value indicating whether processing is finished.
        /// </summary>
        /// <value><c>true</c> if processing is finished; otherwise, <c>false</c>.</value>
        public bool ProcessingFinished { get; private set; }

        /// <summary>
        /// Processes an incoming request.
        /// </summary>
        public void Process()
        {
            OnAuthenticateRequest();
            
            // TODO: add authorization.
            OnMapRequestHandler();
            OnRequestHandlerExecute();
            OnLogRequest();
            _owner.Reuse(this);
        }

        private void OnRequestHandlerExecute()
        {
            if (ProcessingFinished)
            {
                return;
            }

            _handler.Handle(Context, _store);
        }

        private void OnMapRequestHandler()
        {
            if (ProcessingFinished)
            {
                return;
            }

            _handler = _factory.GetHandler(Context.Request);
            if (_handler is NullMessageHandler)
            {
                // TODO: handle error here.
                CompleteProcessing();
            }
        }

        private void OnAuthenticateRequest()
        {
            if (!_provider.AuthenticateRequest(Context))
            {
                // TODO: handle error here.
                // return TRAP saying authenticationFailed.
                CompleteProcessing();
                Context.HandleAuthenticationFailure();
            }
            
            if (Context.Response != null)
            {
                CompleteProcessing();
            }
        }        

        private void OnLogRequest()
        {
            Context.SendResponse();
            if (_logger == null)
            {
                return;
            }

            _logger.Log(Context);
        }

        /// <summary>
        /// Completes the processing.
        /// </summary>
        public void CompleteProcessing()
        {
            ProcessingFinished = true;
        }
    }
}