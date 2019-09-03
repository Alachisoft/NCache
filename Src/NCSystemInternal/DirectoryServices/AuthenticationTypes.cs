using System;
using System.Collections.Generic;
using System.Text;

namespace System.DirectoryServices
{
    //
    // Summary:
    //     The System.DirectoryServices.AuthenticationTypes enumeration specifies the types
    //     of authentication used in System.DirectoryServices. This enumeration has a System.FlagsAttribute
    //     attribute that allows a bitwise combination of its member values.
    [Flags]
    public enum AuthenticationTypes
    {
        //
        // Summary:
        //     Equates to zero, which means to use basic authentication (simple bind) in the
        //     LDAP provider.
        None = 0,
        //
        // Summary:
        //     Requests secure authentication. When this flag is set, the WinNT provider uses
        //     NTLM to authenticate the client. Active Directory Domain Services uses Kerberos,
        //     and possibly NTLM, to authenticate the client. When the user name and password
        //     are a null reference (Nothing in Visual Basic), ADSI binds to the object using
        //     the security context of the calling thread, which is either the security context
        //     of the user account under which the application is running or of the client user
        //     account that the calling thread is impersonating.
        Secure = 1,
        //
        // Summary:
        //     Attaches a cryptographic signature to the message that both identifies the sender
        //     and ensures that the message has not been modified in transit.
        Encryption = 2,
        //
        // Summary:
        //     Attaches a cryptographic signature to the message that both identifies the sender
        //     and ensures that the message has not been modified in transit. Active Directory
        //     Domain Services requires the Certificate Server be installed to support Secure
        //     Sockets Layer (SSL) encryption.
        SecureSocketsLayer = 2,
        //
        // Summary:
        //     For a WinNT provider, ADSI tries to connect to a domain controller. For Active
        //     Directory Domain Services, this flag indicates that a writable server is not
        //     required for a serverless binding.
        ReadonlyServer = 4,
        //
        // Summary:
        //     No authentication is performed.
        Anonymous = 16,
        //
        // Summary:
        //     Specifies that ADSI will not attempt to query the Active Directory Domain Services
        //     objectClass property. Therefore, only the base interfaces that are supported
        //     by all ADSI objects will be exposed. Other interfaces that the object supports
        //     will not be available. A user can use this option to boost the performance in
        //     a series of object manipulations that involve only methods of the base interfaces.
        //     However, ADSI does not verify if any of the request objects actually exist on
        //     the server. For more information, see the topic "Fast Binding Option for Batch
        //     Write/Modify Operations" in the MSDN Library at http://msdn.microsoft.com/library.
        //     For more information about the objectClass property, see the "Object-Class" topic
        //     in the MSDN Library at http://msdn.microsoft.com/library.
        FastBind = 32,
        //
        // Summary:
        //     Verifies data integrity to ensure that the data received is the same as the data
        //     sent. The System.DirectoryServices.AuthenticationTypes.Secure flag must also
        //     be set to use signing.
        Signing = 64,
        //
        // Summary:
        //     Encrypts data using Kerberos. The System.DirectoryServices.AuthenticationTypes.Secure
        //     flag must also be set to use sealing.
        Sealing = 128,
        //
        // Summary:
        //     Enables Active Directory Services Interface (ADSI) to delegate the user's security
        //     context, which is necessary for moving objects across domains.
        Delegation = 256,
        //
        // Summary:
        //     If your ADsPath includes a server name, specify this flag when using the LDAP
        //     provider. Do not use this flag for paths that include a domain name or for serverless
        //     paths. Specifying a server name without also specifying this flag results in
        //     unnecessary network traffic.
        ServerBind = 512
    }
}
