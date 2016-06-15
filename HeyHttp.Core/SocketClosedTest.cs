using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace HeyHttp.Core
{
    class SocketClosedTest
    {
        public static void Start()
        {
            IPHostEntry hostEntry = Dns.GetHostEntry("microsoft.com");
            IPAddress address = hostEntry.AddressList[0];
            int port = 80;
            IPEndPoint remoteEndPoint = new IPEndPoint(address, port);

            Socket client = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(remoteEndPoint);
            client.Close();
            client.Send(new byte[] { 1, 2, 3, 4}); // ObjectDisposedException
        }
    }
}
