using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;

namespace HeyHttp.Core
{
    public class HeyTcpServer
    {
        public const int DefaultPort = 8888;

        public static void Start(HeyTcpServerSettings settings)
        {
            // Dual-stack mode: TcpListerner.Create() listens on both, IPAddress.Any and
            // IPAddress.IPv6Any. "new TcpListener()" do not listen in both.
            TcpListener tcpListener = TcpListener.Create(settings.Port);
            tcpListener.Start();

            while (true)
            {
                Console.WriteLine("Listening on port {0} ...", settings.Port);
                try
                {
                    using (TcpClient client = tcpListener.AcceptTcpClient())
                    {
                        Console.WriteLine("Client connected from {0}\r\n", client.Client.RemoteEndPoint);

                        DoStreamOfGuids(client);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static void DoStreamOfGuids(TcpClient client)
        {
            using (NetworkStream networkStream = client.GetStream())
            {
                for (int i = 0; i < 10; i++)
                {
                    Guid guid = Guid.NewGuid();
                    Console.WriteLine(guid);
                    networkStream.Write(guid.ToByteArray(), 0, 16);
                }

                Guid guidEmpty = Guid.Empty;
                Console.WriteLine(guidEmpty);
                networkStream.Write(guidEmpty.ToByteArray(), 0, 16);
            }
        }
    }
}
