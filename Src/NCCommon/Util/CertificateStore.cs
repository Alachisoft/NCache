//  Copyright (c) 2018 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using Alachisoft.NCache.Common.Communication.Secure;

using System;
using System.Security.Cryptography.X509Certificates;

namespace Alachisoft.NCache.Common.Util
{
    public static class CertificateStore
    {
        internal static X509CertificateCollection GetClientCertificates()
        {
            var cert = GetServerCertificate();
            return cert == null ? new X509CertificateCollection() : new X509CertificateCollection(new [] { cert }); 
        }

        public static X509Certificate2 GetServerCertificate()
        {
            X509Certificate2 cert = null;
            if ((cert = GetCertificateFromSore(new X509Store(StoreName.Root, StoreLocation.LocalMachine))) != null) return cert;
            if ((cert = GetCertificateFromSore(new X509Store(StoreName.Root, StoreLocation.CurrentUser))) != null) return cert;            
            throw new Exception("Couldn't find the required certificate in 'Trusted Root Certificate Authorities (CAs)' store of the 'Local' and 'User' paths.");
        }
        
        public static X509Certificate2 GetCertificateFromSore(X509Store store)
        {
            store.Open(OpenFlags.ReadOnly);
            var cert = FindCertificatByThumbprint(SslConfiguration.Thumbprint, store.Certificates);
            store.Close();
            return cert;
        }

        private static X509Certificate2 FindCertificatByThumbprint(string thumbprint, X509Certificate2Collection certificates)
        {
            var certificate = certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
            if (certificate.Count > 0) return certificate[0];

            foreach (var cert in certificates)
                if (cert.Thumbprint.Equals(thumbprint, StringComparison.InvariantCultureIgnoreCase))
                    return cert;

            return null;
        }
    }
}
