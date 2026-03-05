namespace Alachisoft.NCache.Common.ResponseSerialization
{
    public enum ReposnseBuilderType
    {
        // This type represent the dotnet clients after 5.0 and onwards NCache version, 
        // This Response Builder type considers reposnse type and request id in packet transfer
        // unless any modification required in client communication layer 
        V50DotNet,
        // This type represent the java clients after 5.0 and onwards NCache version, 
        // This Response Builder type also which considers reposnse type and request id in packet transfer
        // but creating of package is differntly.
        // This type change unless any modification required in client communication layer 
        V50Java,
        // This type represents the clients before 5.0 NCache version, 
        // this reposne builder sending packages with only response information. 
        Old
    }

}
