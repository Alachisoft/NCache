using System;
using Alachisoft.NCache.Common;

namespace Alachisoft.NGroups.Protocols
{
    internal class TotalHeaderProvider : ObjectProvider
    {
        public TotalHeaderProvider() { }
        public TotalHeaderProvider(int initialsize) : base(initialsize) { }

        protected override IRentableObject CreateObject()
        {
            return new TOTAL.HDR();
        }
        public override string Name
        {
            get { return "TotalHeaderProvider"; }
        }
        protected override void ResetObject(object obj)
        {
            TOTAL.HDR hdr = obj as TOTAL.HDR;
            if (hdr != null)
            {
                hdr.Reset();
            }
        }

        public override Type ObjectType
        {
            get
            {
                if (_objectType == null) _objectType = typeof(TOTAL.HDR);
                return _objectType;
            }
        }

    }
}