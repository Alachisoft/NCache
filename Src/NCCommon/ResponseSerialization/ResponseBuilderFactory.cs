namespace Alachisoft.NCache.Common.ResponseSerialization
{
    public class ResponseBuilderFactory
    {
        public static IResponseBuilder Create(ResponseBuilderConfiguration responseBuilderConfiguration)
        {
            switch (responseBuilderConfiguration.ReposnseBuilderType)
            {
                case ReposnseBuilderType.V50DotNet:
                    return new V50ResponseBuilder();
                case ReposnseBuilderType.V50Java:
                    return new V50JavaResponseBuilder(); 
                case ReposnseBuilderType.Old:
                    return new OldResponseBuilder();
                default:
                    return new V50ResponseBuilder();
            }
           
        }
    }
}
