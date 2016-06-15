using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace HeyHttp.Core
{
    public class HeyUdpReceiver
    {
        public const int DefaultPort = 5143;

        public static void Start(HeyUdpReceiverSettings settings)
        {
            int port = 5143;

            Start(port);
        }

        private static void Start(int port)
        {
            Socket udpSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

            EndPoint localEP = new IPEndPoint(IPAddress.IPv6Any, port);
            udpSocket.Bind(localEP);

            // UdpClient is commented-out, bacause I haven't been able to make it work in dual-mode.
            //UdpClient udpClient = new UdpClient(port);
            //udpClient.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            ////udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 0); // Just testing.
            ////udpClient.JoinMulticastGroup(IPAddress.Parse("224.0.0.11"));

            Console.WriteLine("Listening on port {0} ...", port);

            byte[] receivedBytes = new byte[UInt16.MaxValue];

            while (true)
            {
                try
                {
                    EndPoint remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);
                    int bytesReceived = udpSocket.ReceiveFrom(receivedBytes, ref remoteEP);

                    //receivedBytes = udpClient.Receive(ref remoteEP);

                    IPEndPoint ipRemoteEP = remoteEP as IPEndPoint;

                    Console.WriteLine("Remote address: {0}", ipRemoteEP.Address);
                    Console.WriteLine("Remote port: {0}", ipRemoteEP.Port);
                    Console.WriteLine("Bytes received: {0}", receivedBytes.Length);
                    Console.WriteLine("Message received: {0}", Encoding.UTF8.GetString(receivedBytes, 0, bytesReceived));
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
