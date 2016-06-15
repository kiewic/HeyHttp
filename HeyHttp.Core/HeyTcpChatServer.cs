using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HeyHttp.Core
{
    public class HeyTcpChatServer
    {
        private static List<TcpClient> connections = new List<TcpClient>();

        public static void Start()
        {
            try
            {
                // TcpListener.Create() listens on IPv4 and IPv6 adapters.
                TcpListener listener = TcpListener.Create(7085);
                listener.Start();

                while (true)
                {
                    TcpClient tcpClient = listener.AcceptTcpClient();
                    lock (connections)
                    {
                        connections.Add(tcpClient);
                    }
                    SendAndReceive(tcpClient);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void SendAndReceive(TcpClient tcpClient)
        {
            Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("Connected: {0}", tcpClient.Client.RemoteEndPoint);

                    using (NetworkStream networkStream = tcpClient.GetStream())
                    {
                        byte version = ReadVersion(networkStream);

                        while (true)
                        {
                            ReadMessage(networkStream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Console.WriteLine("Disconnected.");
            });
        }

        private static byte ReadVersion(NetworkStream networkStream)
        {
            byte[] buffer = new byte[1];
            int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
            Debug.Assert(bytesRead == buffer.Length, "Not enough bytes for version read.");
            Debug.Assert(buffer[0] == 1, "Only version 1 supported.");

            return buffer[0];
        }

        private static void ReadMessage(NetworkStream networkStream)
        {
            byte[] lengthBuffer = new byte[4];
            int bytesRead = networkStream.Read(lengthBuffer, 0, lengthBuffer.Length);
            Debug.Assert(bytesRead == lengthBuffer.Length, "Not enough bytes for length read.");

            uint messageLength = BytesToUInt32(lengthBuffer);

            byte[] messageBuffer = new byte[messageLength];
            bytesRead = networkStream.Read(messageBuffer, 0, messageBuffer.Length);
            Debug.Assert(bytesRead == messageLength, "Not enough bytes for message read.");

            string message = Encoding.UTF8.GetString(messageBuffer);
            Console.WriteLine(message);

            ResendMessage(lengthBuffer, messageBuffer);
        }

        private static uint BytesToUInt32(byte[] buffer)
        {
            Debug.Assert(buffer.Length >= 4);
            uint value = buffer[0];
            for (int i = 1; i < 4; i++)
            {
                value <<= 8;
                value += buffer[i];
            }
            return value;
        }

        private static void ResendMessage(byte[] lengthBuffer,  byte[] messageBuffer)
        {
            // Lock ensures that nobody else remove TcpClients and that the message length and the message
            // are sent together.
            lock (connections)
            {
                for (int i = 0; i < connections.Count;)
                {
                    try
                    {
                        NetworkStream networkStream = connections[i].GetStream();
                        networkStream.Write(lengthBuffer, 0, lengthBuffer.Length);
                        networkStream.Write(messageBuffer, 0, messageBuffer.Length);
                        networkStream.Flush();
                        i++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        connections.RemoveAt(i);
                    }
                }
            }
        }
    }
}
