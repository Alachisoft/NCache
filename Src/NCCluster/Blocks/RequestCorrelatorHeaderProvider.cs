using System;
using Alachisoft.NCache.Common;

namespace Alachisoft.NGroups.Blocks
{
    internal class RequestCorrelatorHeaderProvider : ObjectProvider
    {
        public RequestCorrelatorHeaderProvider() { }
        public RequestCorrelatorHeaderProvider(int initialsize) : base(initialsize) { }

        protected override IRentableObject CreateObject()
        {
            return new RequestCorrelator.HDR();
        }
        public override string Name
        {
            get { return "RequestCorrelatorHeaderProvider";  }
        }
        protected override void ResetObject(object obj)
        {
            RequestCorrelator.HDR hdr = obj as RequestCorrelator.HDR;
            if (hdr != null)
            {
                hdr.Reset();
            }
        }

        public override Type ObjectType
        {
            get 
            {
                if (_objectType == null) _objectType = typeof(RequestCorrelator.HDR);
                return _objectType; 
            }
        }
    }
}