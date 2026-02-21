using System.Security.Cryptography.X509Certificates;

namespace StarterProject.Tools
{
    public class CertificateTools
    {
        public static X509Certificate2 FindCertificate(string location, string thumbprint)
        {
            using var store = new X509Store(location, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            var cert = store.Certificates
                .Find(X509FindType.FindByThumbprint, thumbprint, false)
                .OfType<X509Certificate2>()
                .FirstOrDefault() ?? throw new Exception($"Certificato non trovato. Thumbprint: {thumbprint}");

            if (!cert.HasPrivateKey)
                throw new Exception("Certificato trovato ma non ha chiave privata.");

            return cert;
        }
    }
}
