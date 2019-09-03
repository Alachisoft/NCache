using System;
using System.Security.Cryptography;
using Alachisoft.NCache.Web.SessionState.Interface;

namespace Alachisoft.NCache.Web.SessionState.Utilities
{
    public class SessionKeyGenerator : ISessionKeyGenerator
    {

        RandomNumberGenerator _randomGenerator;
        public SessionKeyGenerator()
        {

            _randomGenerator = RandomNumberGenerator.Create();
        }
        public string Create()
        {
            var bytes = new byte[16];
            _randomGenerator.GetBytes(bytes);
            return new Guid(bytes).ToString();
        }
    }
}
