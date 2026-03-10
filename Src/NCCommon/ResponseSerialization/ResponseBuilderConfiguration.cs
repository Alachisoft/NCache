
namespace Alachisoft.NCache.Common.ResponseSerialization
{
    public class ResponseBuilderConfiguration
    {
        private readonly int _clientVersion;
        private readonly bool _dotNetClient = true;

        public ResponseBuilderConfiguration(int clientVerison, bool dotNetClient= true)
        {
            _clientVersion = clientVerison;
            _dotNetClient = dotNetClient;
        }

        public ReposnseBuilderType ReposnseBuilderType
        {
            get
            {
                var builderType = ReposnseBuilderType.V50DotNet;

                if ( _clientVersion >= 5000)
                {
                     if(!_dotNetClient)
                        builderType= ReposnseBuilderType.V50Java;
                }
                else if(_clientVersion < 5000)
                {
                    builderType= ReposnseBuilderType.Old;
                }
                return builderType;
            }
        }

        
    }
}