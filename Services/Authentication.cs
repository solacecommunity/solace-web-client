using SolaceSystems.Solclient.Messaging;

namespace SolaceWebClient.Services
{
    public class Authentication
    {
        public AuthenticationSchemes Scheme { get; set; } = AuthenticationSchemes.BASIC;

        public string? Username { get; set; }
        public string? Password { get; set; }

        public string? KeystorePath { get; set; }
        public string? KeystorePassphrase { get; set; }

        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Scope { get; set; }
        public string GrantType { get; set; } = "client-credentials";
        public string? IssuerUri { get; set; }

        public byte[]? ClientCertificateBytes { get; set; }
        public string? ClientCertificatePem { get; set; }
        public string? ClientKeyPem { get; set; }
    }
}