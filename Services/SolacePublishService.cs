using Microsoft.AspNetCore.Http;
using SolaceSystems.Solclient.Messaging;
using System.Text;
using ISession = SolaceSystems.Solclient.Messaging.ISession;

namespace SolaceWebClient.Services
{
    public class SolacePublishService : IDisposable
    {
        private IContext? _context;
        private ISession? _session;
        private readonly ILogger<SolacePublishService> _logger;

        public SolacePublishService(ILogger<SolacePublishService> logger)
        {
            _logger = logger;
        }

        public void PublishMessage(string host, string vpnName, Authentication auth, string topic, string message, bool sslVerify)
        {
            ISession _session = null;
            try
            {
                ContextFactoryProperties cfp = new ContextFactoryProperties()
                {
                    SolClientLogLevel = SolLogLevel.Warning
                };
                cfp.LogToConsoleError();
                ContextFactory.Instance.Init(cfp);
                _context = ContextFactory.Instance.CreateContext(new ContextProperties(), null);

                SessionProperties sessionProps = new SessionProperties()
                {
                    Host = host,
                    VPNName = vpnName,
                    AuthenticationScheme = auth.Scheme,
                    UserName = auth.Username,
                    Password = auth.Password,
                    SSLValidateCertificate = sslVerify,
                    SSLTrustStoreDir = "trustedca"
                };

                if (auth.Scheme == AuthenticationSchemes.OAUTH2)
                {
                    OAuth2 oauth2 = new OAuth2();
                    sessionProps.OAuth2AccessToken = oauth2.GetToken(auth);
                }
                // mTLS Support
                if (auth.Scheme == AuthenticationSchemes.CLIENT_CERTIFICATE)
                {
                    if (auth.ClientCertificateBytes != null && auth.ClientCertificateBytes.Length > 0)
                    {
                        sessionProps.SSLClientCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                            auth.ClientCertificateBytes,
                            auth.KeystorePassphrase,
                            System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable |
                            System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.PersistKeySet |
                            System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.MachineKeySet
                        );
                    }
                    else if (!string.IsNullOrWhiteSpace(auth.ClientCertificatePem) && !string.IsNullOrWhiteSpace(auth.ClientKeyPem))
                    {
                        var cert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPem(auth.ClientCertificatePem, auth.ClientKeyPem);
                        if (!string.IsNullOrEmpty(auth.KeystorePassphrase))
                        {
                            cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx, auth.KeystorePassphrase), auth.KeystorePassphrase,
                                System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable | System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet);
                        }
                        sessionProps.SSLClientCertificate = cert;
                    }
                }

                _session = _context.CreateSession(sessionProps, null, null);
                ReturnCode returnCode = _session.Connect();
                if (returnCode != ReturnCode.SOLCLIENT_OK)
                {
                    throw new Exception("Failed to connect to Solace broker.");
                }

                using (IMessage msg = ContextFactory.Instance.CreateMessage())
                {
                    msg.Destination = ContextFactory.Instance.CreateTopic(topic);
                    if (!String.IsNullOrEmpty(message))
                    {
                        msg.BinaryAttachment = Encoding.UTF8.GetBytes(message);
                    }
                    _session.Send(msg);
                }
            }
            catch (OperationErrorException ex)
            {
                _logger.LogError("Exception during PublishMessage: {ex}", ex);
                throw;
            }
            finally
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (_session != null)
            {
                _session.Disconnect();
                _session.Dispose();
                _session = null;
            }

            if (_context != null)
            {
                _context.Dispose();
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
