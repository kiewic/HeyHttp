using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Net.Security;
using System.IO;

namespace HeyHttp.Core
{
    public class HeySslClient
    {
        public static void Start(HeySslClientSettings settings)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(settings.Hostname, settings.Port);

            NetworkStream networkStream = new NetworkStream(socket);
            SslStream sslStream = new SslStream(networkStream);

            sslStream.AuthenticateAsClient(settings.Hostname);

            string requestContent = "";
            byte[] requestBuffer = Encoding.UTF8.GetBytes(requestContent);

            Uri uri = new Uri("https://api.stackexchange.com/2.1/answers?order=desc&sort=activity&site=stackoverflow");
            string requestHeaders = String.Format("{0} {1} HTTP/1.1\r\n" +
                "Host: {2}\r\n" +
                "Content-Type: text/xml\r\n" +
                "Content-Length: {3}\r\n" +
                "Connection: Keep-alive\r\n" +
                "\r\n" +
                "{4}",
                String.IsNullOrEmpty(requestContent) ? "GET" : "POST",
                uri.PathAndQuery,
                uri.Host,
                requestBuffer.Length,
                requestContent);
            requestBuffer = Encoding.UTF8.GetBytes(requestHeaders);

            Console.WriteLine(Encoding.UTF8.GetString(requestBuffer, 0, requestBuffer.Length));
            Console.WriteLine();

            sslStream.Write(requestBuffer, 0, requestBuffer.Length);

            byte[] responseBuffer = new byte[10000];
            int bytesRead = sslStream.Read(responseBuffer, 0, responseBuffer.Length);

            Console.WriteLine(Encoding.UTF8.GetString(responseBuffer, 0, bytesRead));

            socket.Close();
        }
    }
}
