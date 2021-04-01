using Microsoft.AspNetCore.Http;

namespace Alachisoft.NCache.Web.SessionState.Utilities
{
    public class AspCoreEnvironmentContext : IAspEnvironmentContext
    {

        HttpContext _context;
        LocationIdContainer _container;

        public AspCoreEnvironmentContext(HttpContext context)
        {
            _context = context;
            object arg;
            if (_context.Items.TryGetValue(NCacheStatics.LocationIdentifierItemKey, out arg))
            {
                _container = (LocationIdContainer) arg;
            }
            else
            {
                _container = new LocationIdContainer
                {
                    Context = _context
                };

                _context.Items.Add(NCacheStatics.LocationIdentifierItemKey, _container);
                _context.Response.OnStarting(_container.OnResponseStarted, null);

                if (_context.Request.Cookies[NCacheStatics.LocationIdentifier] != null)
                    _container.LocationId = _context.Request.Cookies[NCacheStatics.LocationIdentifier];
            }
        }

        public object Unwrap()
        {
            return _context;
        }

        public object GetItem(object key)
        {
            return _context.Items[key];
        }

        public void StoreItem(object key, object value)
        {
            _context.Items[key] = value;
        }

        public bool ContainsItem(object key)
        {
            return _context.Items.ContainsKey(key);
        }

        public void RemoveItem(object key)
        {
            _context.Items.Remove(key);
        }
        
        public object this[object key]
        {
            get { return _context.Items[key]; }
            set { _context.Items[key] = value; }
        }

        public string GetLocationCookie()
        {
            return _container.LocationId;
        }

        public void SetLocationCookie(string cookieValue)
        {
            _container.LocationId = cookieValue;

            //we can set cookie before response has strated sending. So in SetAndRelease cookie will not be set
            //if (_container.LocationId != null && !_context.Response.HasStarted)
            //    _context.Response.Cookies.Append(NCacheStatics.LocationIdentifier, _container.LocationId);
        }

        public void RemoveLocationCookie()
        {
            _container.LocationId = null;
        }

        public void FinalizeContext()
        {
        }
    }
}
