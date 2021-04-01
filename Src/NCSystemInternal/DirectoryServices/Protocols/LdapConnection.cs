using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Novell.Directory.Ldap;

namespace System.DirectoryServices.Protocols
{
    //
    // Summary:
    //     The System.DirectoryServices.Protocols.LdapConnection class creates a TCP/IP
    //     or UDP LDAP connection to Microsoft Active Directory Domain Services or an LDAP
    //     server.
    public class LdapConnection : /*DirectoryConnection, */IDisposable
    {
        Novell.Directory.Ldap.LdapConnection _ldapConnection;
        //
        // Summary:
        //     The System.DirectoryServices.Protocols.LdapConnection.#ctor(System.String) constructor
        //     creates an instance of the System.DirectoryServices.Protocols.LdapConnection
        //     class using the specified server.
        //
        // Parameters:
        //   server:
        //     A string specifying the server which can be a domain name, LDAP server name or
        //     dotted strings representing the IP address of the LDAP server. Optionally, this
        //     parameter may also include a port number, separated from the right end of the
        //     string by a colon (:).
        //
        // Exceptions:
        //   T:System.DirectoryServices.Protocols.LdapException:
        //     Thrown if it fails to create a connection block or fails to open a connection
        //     to server.
        public LdapConnection(string server)
        {
            //TODO: ALACHISOFT
            _ldapConnection = new Novell.Directory.Ldap.LdapConnection();
        }

        //
        // Summary:
        //     The System.DirectoryServices.Protocols.LdapConnection.Finalize method allows
        //     an System.DirectoryServices.Protocols.LdapConnection object to attempt to free
        //     resources and perform other cleanup operations before the System.DirectoryServices.Protocols.LdapConnection
        //     object is reclaimed by garbage collection.

        //
        // Summary:
        //     The System.DirectoryServices.Protocols.LdapConnection.AutoBind property specifies
        //     whether an automatic bind is allowed.
        //
        // Returns:
        //     true if the automatic bind is allowed; otherwise, false.
        public bool AutoBind { get; set; }

        //
        // Summary:
        //     The System.DirectoryServices.Protocols.LdapConnection.Abort(System.IAsyncResult)
        //     method cancels the asynchronous request.
        //
        // Parameters:
        //   asyncResult:
        //     A System.IAsyncResult object that references the asynchronous request.
        //
        // Exceptions:
        //   T:System.ObjectDisposedException:
        //     The object handle is not valid.
        //
        //   T:System.ArgumentNullException:
        //     asyncResult is null (Nothing in Visual Basic).
        //
        //   T:System.ArgumentException:
        //     Thrown if asyncResult was not returned by the corresponding call to System.DirectoryServices.Protocols.LdapConnection.BeginSendRequest
        public void Abort(IAsyncResult asyncResult)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     The System.DirectoryServices.Protocols.LdapConnection.Bind method sends an LDAP
        //     bind using the current credentials.
        //
        // Exceptions:
        //   T:System.ObjectDisposedException:
        //     The object handle is not valid.
        //
        //   T:System.DirectoryServices.Protocols.LdapException:
        //     The error code returned by LDAP does not map to one of the System.DirectoryServices.Protocols.ResultCode
        //     enumeration error codes.
        //
        //   T:System.InvalidOperationException:
        //     Either the System.DirectoryServices.Protocols.DirectoryConnection.ClientCertificates
        //     property specifies more than one client certificate to send for authentication,
        //     or the System.DirectoryServices.Protocols.LdapConnection.AuthType property is
        //     Anonymous and one or more credentials are supplied.
        public void Bind()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     The System.DirectoryServices.Protocols.LdapConnection.Bind(System.Net.NetworkCredential)
        //     method sends an LDAP bind using the specified System.Net.NetworkCredential.
        //
        // Parameters:
        //   newCredential:
        //     A System.Net.NetworkCredential object that specifies the credentials to use.
        //
        // Exceptions:
        //   T:System.ObjectDisposedException:
        //     The object handle is not valid.
        //
        //   T:System.DirectoryServices.Protocols.LdapException:
        //     The error code returned by LDAP does not map to a System.DirectoryServices.Protocols.ResultCode
        //     enumeration error code.
        //
        //   T:System.InvalidOperationException:
        //     Either the System.DirectoryServices.Protocols.DirectoryConnection.ClientCertificates
        //     property specifies more than one client certificate to send for authentication,
        //     or the System.DirectoryServices.Protocols.LdapConnection.AuthType property is
        //     Anonymous and one or more credentials are supplied.
        public void Bind(NetworkCredential newCredential)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     The System.DirectoryServices.Protocols.LdapConnection.Dispose method closes and
        //     releases the LDAP handle.
        public void Dispose()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     The System.DirectoryServices.Protocols.LdapConnection.Dispose(System.Boolean)
        //     method closes the connection and optionally releases the LDAP handle.
        //
        // Parameters:
        //   disposing:
        //     true if the handle is released when the connection is closed or false if the
        //     connection is closed without releasing the handle.
        protected virtual void Dispose(bool disposing)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
    }
}
