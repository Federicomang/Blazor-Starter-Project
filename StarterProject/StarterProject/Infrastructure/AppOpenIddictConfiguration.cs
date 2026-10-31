using StarterProject.Tools;

namespace StarterProject.Infrastructure
{
    public class AppOpenIddictConfiguration
    {
        public bool IsSecure { get; set; }
        public string CertificateType { get; set; } = string.Empty;

        public ThumbprintOptions? Thumbprint { get; set; } = new();
        public FileOptions? File { get; set; } = new();

        private void ConfigureByThumbprint(OpenIddictServerBuilder opt)
        {
            if (Thumbprint == null)
            {
                opt.AddDevelopmentEncryptionCertificate();
                opt.AddDevelopmentSigningCertificate();
            }
            else
            {
                if (string.IsNullOrEmpty(Thumbprint.CertificatePath))
                {
                    if (string.IsNullOrEmpty(Thumbprint.EncryptionCertificate))
                        opt.AddDevelopmentEncryptionCertificate();
                    else
                        opt.AddEncryptionCertificate(Thumbprint.EncryptionCertificate);

                    if (string.IsNullOrEmpty(Thumbprint.SigningCertificate))
                        opt.AddDevelopmentSigningCertificate();
                    else
                        opt.AddSigningCertificate(Thumbprint.SigningCertificate);
                }
                else
                {
                    if (string.IsNullOrEmpty(Thumbprint.EncryptionCertificate))
                        opt.AddDevelopmentEncryptionCertificate();
                    else
                        opt.AddEncryptionCertificate(CertificateTools.FindCertificate(Thumbprint.CertificatePath, Thumbprint.EncryptionCertificate));

                    if (string.IsNullOrEmpty(Thumbprint.SigningCertificate))
                        opt.AddDevelopmentSigningCertificate();
                    else
                        opt.AddSigningCertificate(CertificateTools.FindCertificate(Thumbprint.CertificatePath, Thumbprint.SigningCertificate));
                }
            }
        }

        private void ConfigureByFile(OpenIddictServerBuilder opt)
        {
            if (File == null)
            {
                opt.AddDevelopmentEncryptionCertificate();
                opt.AddDevelopmentSigningCertificate();
            }
            else
            {
                if (string.IsNullOrEmpty(File.EncryptionCertificate))
                    opt.AddDevelopmentEncryptionCertificate();
                else
                    opt.AddEncryptionCertificate(CertificateTools.GetCertificateFromFile(File.EncryptionCertificate, File.EncryptionCertificatePassword));

                if (string.IsNullOrEmpty(File.SigningCertificate))
                    opt.AddDevelopmentSigningCertificate();
                else
                    opt.AddSigningCertificate(CertificateTools.GetCertificateFromFile(File.SigningCertificate, File.SigningCertificatePassword));
            }
        }

        public void Configure(OpenIddictServerBuilder opt)
        {
            switch (CertificateType.ToLower())
            {
                case "thumbprint":
                    ConfigureByThumbprint(opt);
                    break;
                case "file":
                    ConfigureByFile(opt);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public class ThumbprintOptions
    {
        public string SigningCertificate { get; set; } = string.Empty;
        public string EncryptionCertificate { get; set; } = string.Empty;
        public string CertificatePath { get; set; } = string.Empty;
    }

    public class FileOptions
    {
        public string SigningCertificate { get; set; } = string.Empty;
        public string EncryptionCertificate { get; set; } = string.Empty;
        public string SigningCertificatePassword { get; set; } = string.Empty;
        public string EncryptionCertificatePassword { get; set; } = string.Empty;
    }
}
