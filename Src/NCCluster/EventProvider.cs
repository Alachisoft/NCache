using System;
using Alachisoft.NCache.Common;

namespace Alachisoft.NGroups
{
    internal class EventProvider : ObjectProvider
    {
        public EventProvider() { }
        public EventProvider(int initialsize) : base(initialsize) { }

        protected override IRentableObject CreateObject()
        {
            return new Event();
        }
        public override string Name
        {
            get { return "EventProvider"; }
        }
        protected override void ResetObject(object obj)
        {
            Event hdr = obj as Event;
            if (hdr != null) hdr.Reset();
        }

        public override Type ObjectType
        {
            get
            {
                if (_objectType == null) _objectType = typeof(Event);
                return _objectType;
            }
        }
    }
}