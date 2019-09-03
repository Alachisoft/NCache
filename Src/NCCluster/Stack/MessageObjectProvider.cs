using System;
using Alachisoft.NCache.Common;

namespace Alachisoft.NGroups.Stack
{
    public class MessageObjectProvider : ObjectProvider
    {
        public MessageObjectProvider() { }
        public MessageObjectProvider(int initialsize) : base(initialsize) { }
        protected override IRentableObject CreateObject()
        {
            return new Message();
        }

        public override string Name
        {
            get { return "MessageObjectProvider"; }
        }
        protected override void ResetObject(object obj)
        {
            Message msg = obj as Message;
            if (msg != null)
            {
                msg.reset();
            }
        }

        public override Type ObjectType
        {
            get
            {
                if (_objectType == null) _objectType = typeof(Message);
                return _objectType;
            }
            
        }
    }
}