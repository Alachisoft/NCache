#if NETCORE
using System.Diagnostics.Tracing;
#else
using Alachisoft.NCache.Common.Tracing.Tracer;
#endif

namespace Alachisoft.NCache.Common.Tracing
{
    public delegate void OnEventSourceEnabled();
    public delegate void OnEventSourceDisabled();
    public class EventSourceBase : EventSource

    {
        private event OnEventSourceEnabled _enventSoureceEanbled;
        private event OnEventSourceDisabled _enventSoablished;

        public event OnEventSourceEnabled EventSourceEanbleEvent
        {
            add { _enventSoureceEanbled += value; }
            remove { _enventSoureceEanbled -= value; }
        }

        public event OnEventSourceDisabled EventSourceDisableEvent
        {
            add { _enventSoablished += value; }
            remove { _enventSoablished -= value; }
        }

        public EventSourceBase()
        {
#if NETCORE
            this.EventCommandExecuted += OnEventSourceCommand;
#endif
        }

#if NETCORE

        private void OnEventSourceCommand(object sender, EventCommandEventArgs e)
        {
            if (e.Command == EventCommand.Enable && _enventSoureceEanbled != null)
                _enventSoureceEanbled.Invoke();

            if (e.Command == EventCommand.Disable && _enventSoablished != null)
                _enventSoablished.Invoke();
        }
#endif
        public bool IsDebugEnabled
        {
            get { return IsEnabled(EventLevel.Verbose, EventKeywords.All); }
        }


        public bool IsInfoEnabled
        {
            get { return IsEnabled(EventLevel.Informational, EventKeywords.All); }
        }

        public bool IsCriticalEnabled
        {
            get { return IsEnabled(EventLevel.Critical, EventKeywords.All); }
        }

        public bool IsWarnEnabled
        {
            get { return IsEnabled(EventLevel.Warning, EventKeywords.All); }
        }

        public bool IsErrorEnabled
        {
            get { return IsEnabled(EventLevel.Error, EventKeywords.All); }
        }
    }
}