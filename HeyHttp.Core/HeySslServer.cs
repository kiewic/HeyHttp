using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace HeyHttp.Core
{
    public class HeySslServer
    {
        public const int DefaultPort = 443;
        private static X509Certificate2 serverCertificate;
        private static HeySslServerSettings settings;


        public static void Start(HeySslServerSettings settings)
        {
            HeySslServer.settings = settings;
            Start();
        }

        private static void Start()
        {
            using (Socket socketListener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                // Set dual mode socket, so it can receive connections in both, IPv4 and IPv6.
                socketListener.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

                SelectCertificate();

                socketListener.Bind(new IPEndPoint(IPAddress.IPv6Any, settings.Port));
                socketListener.Listen(5);

                Console.WriteLine("Listening on port {0} ...", settings.Port);

                socketListener.BeginAccept(OnAccept, socketListener);

                // Don't let the main thread finish.
                AutoResetEvent autoResetEvent = new AutoResetEvent(false);
                autoResetEvent.WaitOne();
            }
        }

        private static void OnAccept(IAsyncResult asyncResult)
        {
            var logger = GlobalSettings.LoggerFactory.GetSessionLogger();
            Socket socketListener = asyncResult.AsyncState as Socket;
            SslStream sslStream = null;
            string clientName = null;

            try
            {
                using (Socket clientSocket = socketListener.EndAccept(asyncResult))
                {
                    socketListener.BeginAccept(OnAccept, socketListener);

                    clientName = clientSocket.RemoteEndPoint.ToString();
                    logger.WriteTransportLine(String.Format("Client connected from {0}\r\n", clientName));

                    NetworkStream networkStream = new NetworkStream(clientSocket);

                    // Plain connection before upgrading to SSL?
                    //message = ReadMessage(networkStream);
                    //Console.WriteLine(message);

                    RemoteCertificateValidationCallback callback = null;
                    if (settings.ClientCertificateRequired)
                    {
                        callback = new RemoteCertificateValidationCallback(ValidateClientCertificate);
                    }

                    sslStream = new SslStream(
                        networkStream,
                        false,
                        callback,
                        null);

                    sslStream.AuthenticateAsServer(
                        serverCertificate,
                        settings.ClientCertificateRequired,
                        SslProtocols.Default,
                        false);

                    switch (settings.Protocol)
                    {
                        case ApplicationLayerProtocol.Http:
                            DoHttp(logger, sslStream, clientSocket);
                            break;
                        case ApplicationLayerProtocol.Ws:
                            DoWs(logger, sslStream, clientSocket);
                            break;
                        default:
                            DoUndefined(sslStream);
                            break;
                    }

                }
            }
            catch (Exception ex)
            {
                logger.WriteErrorLine(ex.Message);
            }
            finally
            {
                logger.WriteTransportLine(String.Format("Disconnected from {0}\r\n", clientName));

                if (sslStream != null)
                {
                    SslClientCertificateInfo.Remove(sslStream.GetHashCode());
                }
            }
        }

        private static bool ValidateClientCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors errors)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendFormat("SslPolicyErrors: {0}\r\n", errors);

            if (certificate != null)
            {
                builder.AppendFormat("Subject: {0}\r\n", certificate.Subject);
                builder.AppendFormat("Issuer: {0}\r\n", certificate.Issuer);
                builder.AppendFormat("Hash: {0}", BitConverter.ToString(certificate.GetCertHash()));
            }

            Console.WriteLine(builder.ToString());

            SslStream sslStream = sender as SslStream;
            SslClientCertificateInfo.Add(sslStream.GetHashCode(), builder.ToString());

            if (errors == SslPolicyErrors.RemoteCertificateNotAvailable)
            {
                // Do not allow the server to communicate with unauthenticated clients.
                return false;
            }

            return true;
        }

        private static void DoHttp(HeyLogger logger, Stream sslStream, Socket clientSocket)
        {
            HeyHttpServerThread connection = new HeyHttpServerThread(
                logger,
                clientSocket,
                null);
            connection.Accept(sslStream);
        }

        private static void DoWs(HeyLogger logger, Stream sslStream, Socket clientSocket)
        {
            HeyWebSocketServer.Accept(logger, sslStream);
        }

        private static void DoUndefined(SslStream sslStream)
        {
            byte[] buffer = new byte[10000];
            int bytesRead = sslStream.Read(buffer, 0, buffer.Length);
            Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, bytesRead));

            string response = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\nHi";
            sslStream.Write(Encoding.UTF8.GetBytes(response));
        }

        private static void SelectCertificate()
        {
            serverCertificate = GetCertificateFromStore(settings.Thumbprint, false);
        }

        // For instructions about creating and installing the necessary certificates, read Certificates.txt
        private static X509Certificate2 GetCertificateFromStore(string thumbprint, bool validOnly)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection allCerts = store.Certificates;
            Console.WriteLine(
                "{0} certificates in {1} store at {2} location.",
                allCerts.Count,
                store.Name,
                store.Location);

            // Third argument of Certificates.Find defines if the certificate must or must not be valid.
            // The issuer of the certificate must be in the 'root certification authorities' store
            // for the certificate to be valid.
            // In the case of self-signed certificates, the certificate must  be in 'my' store as well as
            // 'root certification authorities' store to be valid.
            X509Certificate2Collection matchingCerts = allCerts.Find(
                X509FindType.FindByThumbprint,
                thumbprint,
                false);
            Console.WriteLine("{0} certificates with {1} thumbprint.", matchingCerts.Count, thumbprint);

            if (matchingCerts.Count == 0)
            {
                foreach (var cert in allCerts)
                {
                    Console.WriteLine(
                        "{0} {1} {2}",
                        cert.Thumbprint,
                        cert.SubjectName.Name,
                        cert.IssuerName.Name);
                }
                throw new Exception("Certificate not found.");
            }

            return matchingCerts[0];
        }
    }
}
