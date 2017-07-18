using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Web;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HeyHttp.Core
{
    delegate void RemoveConnectionDelegate(HeyHttpServerThread connection);

    public class HeyHttpServer
    {
        public const int DefaultPort = 80;
        private static object connectionsLock = new object();
        private static List<HeyHttpServerThread> connections = new List<HeyHttpServerThread>();
        private static HeyHttpServerSettings settings;

        public static void Start(HeyHttpServerSettings settings)
        {
            HeyHttpServer.settings = settings;
            Start();
        }

        private static void Start()
        {
            // Trap CTRL + C.
            Console.CancelKeyPress += OnCancelKeyPress;

            // Dual-stack mode: TcpListerner.Create() listens on both, IPAddress.Any and IPAddress.IPv6Any.
            TcpListener tcpListener = TcpListener.Create(settings.Port);
            tcpListener.Start();
            Console.WriteLine("Listening on port {0} ...", settings.Port);

            while (true)
            {
                try
                {
                    var logger = GlobalSettings.LoggerFactory.GetSessionLogger();
                    logger.IsBodyLogEnabled = !settings.OnlyHeadersLog;

                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    HeyHttpServerThread connection = new HeyHttpServerThread(
                        logger,
                        tcpClient.Client,
                        RemoveConnection);

                    lock (connectionsLock)
                    {
                        connections.Add(connection);
                    }

                    connection.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void RemoveConnection(HeyHttpServerThread connection)
        {
            lock (connectionsLock)
            {
                connections.Remove(connection);
            }
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            // CTRL + C is handled. CTRL + Break is not handled.
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                // Lock sections that interact with "connections".
                lock (connectionsLock)
                {
                    foreach (HeyHttpServerThread connection in connections)
                    {
                        connection.Kill();
                    }
                    connections.Clear();
                }
                Console.WriteLine("To kill the app press CTRL + Break.");
                e.Cancel = true;
            }
        }
    }
}
