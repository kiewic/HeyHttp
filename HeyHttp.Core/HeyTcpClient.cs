using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;

namespace HeyHttp.Core
{
    public class HeyTcpClient
    {
        public const int DefaultPort = 8080;

        public static void Start(HeyTcpClientSettings settings)
        {
            try
            {
                TcpClient tcpClient = new TcpClient(settings.Hostname, settings.Port);
                NetworkStream networkStream = tcpClient.GetStream();

                byte[] buffer;
                byte[] lengthBuffer;

                string message1 = "This is the Euro sign: \u20AC";
                buffer = Encoding.UTF8.GetBytes(message1);
                lengthBuffer = BitConverter.GetBytes(buffer.Length);
                Debug.Assert(lengthBuffer.Length == 4);
                Array.Reverse(lengthBuffer); // Convert it to big-endian.

                networkStream.Write(lengthBuffer, 0, 3); // split the data in two Write operations
                networkStream.Write(lengthBuffer, 3, 1); // second part
                networkStream.Write(buffer, 0, buffer.Length);

                string message2 = "Cents character: \u00a2";
                buffer = Encoding.UTF8.GetBytes(message2);
                lengthBuffer = BitConverter.GetBytes(buffer.Length);
                Debug.Assert(lengthBuffer.Length == 4);
                Array.Reverse(lengthBuffer); // Convert it to big-endian.

                networkStream.Write(lengthBuffer, 0, lengthBuffer.Length);
                networkStream.Write(buffer, 0, buffer.Length);

                tcpClient.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


    }
}
