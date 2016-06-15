using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HeyHttp.Core
{
    public class HeyProxyServer
    {
        public const int DefaultPort = 3128;
        private static object thisLock = new object();
        private static HeyProxyServerSettings settings;

        public static void Start(HeyProxyServerSettings settings)
        {
            HeyProxyServer.settings = settings;

            Socket ipv4Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ipv4Listener.Bind(new IPEndPoint(IPAddress.Any, settings.Port));
            ipv4Listener.Listen(10);
            ipv4Listener.BeginAccept(OnAccept, ipv4Listener);

            Socket ipv6Listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            ipv6Listener.Bind(new IPEndPoint(IPAddress.IPv6Any, settings.Port));
            ipv6Listener.Listen(10);
            ipv6Listener.BeginAccept(OnAccept, ipv6Listener);

            Console.WriteLine("Listening on port {0} ...", settings.Port);

            // Don't let the main thread finish.
            AutoResetEvent autoResetEvent = new AutoResetEvent(false);
            autoResetEvent.WaitOne();
        }

        private static void OnAccept(IAsyncResult asyncResult)
        {
            HeyLogger logger = new HeyLogger();
            Socket socketListener = asyncResult.AsyncState as Socket;
            Socket client = socketListener.EndAccept(asyncResult);
            socketListener.BeginAccept(OnAccept, asyncResult.AsyncState);

            try
            {
                HeyHttpRequest request = new HeyHttpRequest(logger);

                // Client got connected.
                logger.WriteLine(String.Format("Connected: {0}", client.RemoteEndPoint));

                // Read HTTP headers.
                NetworkStream clientStream = new NetworkStream(client);
                MemoryStream tempStream = new MemoryStream();
                request.ReadHeaders(clientStream, tempStream);

                // Authentication.
                if (settings.AuthenticationRequired && !IsAuthenticated(request))
                {
                    Reply407AndClose(logger, clientStream);
                    return;
                }

                // Find server host name.
                logger.WriteLine(String.Format("Trying to connect to {0}", request.Host));

                // Get IP address for the given server hostname.
                IPHostEntry hostEntry = Dns.GetHostEntry(request.Host);
                if (hostEntry.AddressList.Length == 0)
                {
                    throw new Exception("Unknow server hostname.");
                }
                IPAddress address = hostEntry.AddressList[0];

                // Connect to server.
                Socket server = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                server.Connect(new IPEndPoint(address, request.Port));

                logger.WriteLine(String.Format("Connected to {0}", request.Host));

                // Send cached data to server.
                NetworkStream serverStream = new NetworkStream(server);

                // When using the CONNECT method, proxy must send a HTTP response to client.
                // See "Tunneling TCP based protocols through Web proxy servers" internet-draft.
                if (request.Method == "CONNECT")
                {
                    HeyHttpResponse response = new HeyHttpResponse(logger);
                    response.Status = "200 Connection established";
                    response.Headers.Add("Proxy-agent: Happy-Sockets-Proxy/1.0");
                    response.CopyHeadersTo(clientStream);
                }
                else
                {
                    // Only forward headers when it is not a CONNECT request.
                    tempStream.Seek(0, SeekOrigin.Begin);
                    tempStream.CopyTo(serverStream);
                }

                // Forward data.
                ParameterizedThreadStart serverToClientStart = new ParameterizedThreadStart(ForwardData);
                Thread serverToClientThread = new Thread(serverToClientStart);
                serverToClientThread.Start(
                    new StreamsPair
                    {
                        StreamA = serverStream,
                        StreamB = clientStream,
                        Label = "server to client",
                        Logger = logger
                    });

                ParameterizedThreadStart clientToServerStart = new ParameterizedThreadStart(ForwardData);
                Thread clientToServerThread = new Thread(clientToServerStart);
                clientToServerThread.Start(
                    new StreamsPair
                    {
                        StreamA = clientStream,
                        StreamB = serverStream,
                        Label = "client to server",
                        Logger = logger
                    });

                //serverToClientThread.Join();
                //clientToServerThread.Join();

                // TODO: make sure streams do not go out of scope.
                // TODO: wait until threads end and ensure connections are close.
            }
            catch (SocketException ex)
            {
                WriteLine(ConsoleColor.Red, ex.Message);
                logger.WriteLine(ex.ToString());

                // We couldn't connect to the  server, terminate connection with the client.
                client.Close();
            }
            catch (IOException ex)
            {
                WriteLine(ConsoleColor.Red, ex.Message);
                logger.WriteLine(ex.ToString());

                // The client closed the connection, terminate the connection with the server.
                client.Close();
            }
            catch (Exception ex)
            {
                WriteLine(ConsoleColor.Red, ex.Message);
                logger.WriteLine(ex.ToString());
            }
        }

        private static bool IsAuthenticated(HeyHttpRequest request)
        {
            string authorization = request.GetHeader("Proxy-Authorization");
            if (!String.IsNullOrEmpty(authorization))
            {
                // Credentails are included.
                return true;
            }

            return false;
        }

        private static void Reply407AndClose(HeyLogger logger, NetworkStream clientStream)
        {
            HeyHttpResponse response = new HeyHttpResponse(logger);
            response.Version = "HTTP/1.0";
            response.Status = "407 Proxy Authentication Required";
            response.Headers.Add("Proxy-agent: Netscape-Proxy/1.1");
            response.Headers.Add("Proxy-Authenticate: Basic realm=\"WallyWorld\"");
            response.Headers.Add("Content-Length: 0");
            response.Headers.Add("Connection: close");
            response.CopyHeadersTo(clientStream);
            clientStream.Close();
        }

        private static void WriteLine(ConsoleColor newForegroundColor, string label)
        {
            lock (thisLock)
            {
                ConsoleColor oldForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = newForegroundColor;
                Console.WriteLine(label);
                Console.ForegroundColor = oldForegroundColor;
            }
        }

        private static void ForwardData(object args)
        {
            StreamsPair pair = args as StreamsPair;
            HeyLogger logger = pair.Logger;
            byte[] buffer = new byte[1024 * 1024];

            try
            {
                while (pair.StreamA.CanRead)
                {
                    int bytesRead = pair.StreamA.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        logger.WriteLine(String.Format("No more bytes to read from {0}.", pair.Label));
                        break;
                    }

                    pair.StreamB.Write(buffer, 0, bytesRead);
                    logger.WriteLine(String.Format("{0} bytes from {1}.", bytesRead, pair.Label));
                }
            }
            catch (IOException ex)
            {
                WriteLine(ConsoleColor.Red, pair.Label);
                logger.WriteLine(ex.ToString());
            }

            // TODO: Check this is right.
            // Tell the other side connections is being shutdown.
            pair.StreamB.Close();
        }

        private class StreamsPair
        {
            public NetworkStream StreamA;
            public NetworkStream StreamB;
            public HeyLogger Logger;
            public string Label;
        }
    }
}
