using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Alachisoft.NCache.Web.SessionState.Interface;
using Microsoft.AspNetCore.Http;

namespace Alachisoft.NCache.Web.SessionState
{
    public class NCacheSession : ISession
    {
        NCacheSessionData _data;
        readonly HttpContext _context;
        string _sessionId;
        readonly ISessionStoreService _cacheStorage;
        readonly bool _readonly;
        readonly bool _newSession;
        readonly TimeSpan _timeoutSpan;
        bool _locked;
        object _lockId;
        TimeSpan _lockAge;
        readonly TimeSpan _sessionTimeout;
        bool _isDirty, _modified;

        internal NCacheSession(HttpContext context, string sessionId, ISessionStoreService cacheStorage, bool readOnly, bool newSession, int requestTimeout, TimeSpan sessionTimeout)
        {
            _sessionId = sessionId;
            _context = context;
            _data = new NCacheSessionData();
            _cacheStorage = cacheStorage;
            _readonly = readOnly;
            _newSession = newSession;
            _timeoutSpan = TimeSpan.FromSeconds(requestTimeout);
            _sessionTimeout = sessionTimeout;
        }
        
        public async Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            SessionInitializationActions actions;

            if (_readonly)
            {
                _data =
                    _cacheStorage.GetItem(_context, _sessionId, out _locked, out _lockAge, out _lockId, out actions) as
                        NCacheSessionData;
            }
            else
            {
                _data =
                    _cacheStorage.GetItemExclusive(_context, _sessionId, out _locked, out _lockAge, out _lockId,
                        out actions) as NCacheSessionData;
            }
            if (_data == null)
            {
                if (_locked)
                {
                    PollAndRetryGetItemExclusive();
                }
                else
                {
                    _data = new NCacheSessionData();
                    NewSession = true;
                }
            }

            if (_data != null && _data.Items.ContainsKey(NCacheStatics.EmptySessionFlag))
                EmptySession = NewSession = true;

            IsAvailable = true;
            _isDirty = false;
        }

        private void PollAndRetryGetItemExclusive()
        {
            SessionInitializationActions actions;
            if (!_readonly)
            {
                while (_locked && _lockAge < _timeoutSpan)
                {
                    Thread.Sleep(500);
                    _data = _cacheStorage.GetItemExclusive(_context, _sessionId, out _locked, out _lockAge, out _lockId,
                        out actions) as NCacheSessionData;
                }
                if (_locked && _lockAge > _timeoutSpan)
                {
                    _cacheStorage.ReleaseItemExclusive(_context, _sessionId, _lockId);
                    _data = _cacheStorage.GetItemExclusive(_context, _sessionId, out _locked, out _lockAge, out _lockId,
                        out actions) as NCacheSessionData;
                    if (_locked)
                    {
                        PollAndRetryGetItemExclusive();
                    }
                }
            }
        }


        public async Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_readonly)
            {
                if (IsAvailable)
                {
                    if (EmptySession)
                    {
                        if (_isDirty)
                        {
                            SessionInitializationActions actions;
                            _data = _cacheStorage.GetItemExclusive(_context, _sessionId, out _locked, out _lockAge, out _lockId,
                                out actions) as NCacheSessionData;
                            _cacheStorage.SetAndReleaseItemExclusive(_context, _sessionId, _data, _lockId, _newSession,
                                Convert.ToInt32(_sessionTimeout.TotalMinutes));
                            _isDirty = false;
                        }
                    }
                    else
                    {
                        if (_isDirty)
                        {
                            _cacheStorage.SetAndReleaseItemExclusive(_context, _sessionId, _data, _lockId, _newSession,
                                Convert.ToInt32(_sessionTimeout.TotalMinutes));
                            _isDirty = false;
                        }
                        else
                        {
                            _cacheStorage.ReleaseItemExclusive(_context, _sessionId, _lockId);
                        }
                    }
                    IsAvailable = false;
                }
            }
        }

        public async Task AbandonAsync()
        {
            if (!IsAvailable)
                await LoadAsync();

            _cacheStorage.RemoveItem(_context, _sessionId, _lockId);
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            if (!IsAvailable)
                LoadAsync();
            value = null;
            return _data != null && _data.Items.TryGetValue(key, out value);
        }

        public void Set(string key, byte[] value)
        {
            if (!IsAvailable)
                LoadAsync();

            if (_data == null) return;

            if (!_data.Items.ContainsKey(key))
                _data.Items.Add(key, value);
            else
                _data.Items[key] = value;

            _isDirty = _modified = true;
        }

        public void Remove(string key)
        {
            if (!IsAvailable)
                LoadAsync();

            if (_data == null) return;
            _isDirty = _modified |= _data.Items.Remove(key);
        }

        public void Clear()
        {
            if (!IsAvailable)
                LoadAsync();

            if (_data == null) return;
            _isDirty = _modified |= _data.Items.Count > 0;
            _data.Items.Clear();
        }

        public bool IsAvailable { get; internal set; }
        public string Id { get { return _sessionId; } }
        public IEnumerable<string> Keys { get { return _data.Items.Keys; } }

        //The Modified flag is separate because the user has the ability to commit the session, which clears the dirty flag.
        internal bool WasModified { get { return _modified; } }

        //This flag is needed because if the session is not new, it should be committed, i.e. if it is not dirty, it's lock should be released. 
        internal bool NewSession { get; private set; }

        //Special case for empty session. The session is not locked but if this session is modified, it should be forcibly applied against the sessionId
        //even of a session against the sessionId exists.
        internal bool EmptySession { get; private set; }

    }
}
