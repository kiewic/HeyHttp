using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;

namespace HeyHttp.Core
{
    public class HeyHttpClient
    {
        public static void Start(HeyHttpClientSettings settings)
        {
            try
            {
                Uri uri = new Uri(settings.UriString);

                //using (TcpClient client = new TcpClient(AddressFamily.InterNetworkV6))
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(uri.Host, uri.Port);
                    //client.Connect(IPAddress.Parse("2001:4898:1b:4:71e6:90b3:efa0:aa9b"), 80);

                    Stream stream = client.GetStream();

                    if (uri.Port == 443)
                    {
                        SslStream sslStream = new SslStream(stream, true);
                        sslStream.AuthenticateAsClient(uri.Host);
                        stream = sslStream;
                    }

                    HeyHttpRequest request = new HeyHttpRequest(null);

                    if (!String.IsNullOrEmpty(settings.Method))
                    {
                        request.Method = settings.Method;
                    }
                    else
                    {
                        request.Method = "GET";
                    }

                    request.PathAndQuery = uri.PathAndQuery;
                    request.Version = "HTTP/1.1";
                    request.SetHeader("Host", uri.Host);
                    request.SetHeader("Accept-Encoding", "gzip");
                    request.SetHeader("Connection", "Keep-Alive");

                    foreach (var header in settings.Headers)
                    {
                        if (!String.IsNullOrEmpty(header))
                        {
                            request.SetHeader(header);
                        }
                    }

                    byte[] requestBytes = Encoding.ASCII.GetBytes(request.ConcatenateHeaders());

                    // This is a sample on how to overwrite the command line request:
                    //StringBuilder builder = new StringBuilder();
                    //builder.Append("POST /v5.0/folder.a4fb14adbccd1917.A4FB14ADBCCD1917!32089/files HTTP/1.1\r\n");
                    //builder.Append("Accept-Encoding: \r\n");
                    //builder.Append("Authorization: Bearer EwCIAq1DBAAUGCCXc8wU/zFu9QnLdZXy%2bYnElFkAAdYrLSFwx5TYQJSLo7JvCeKfAO384WRQiHYnTK8qTQDUiiVU2H5/sLAM1/j5gfRCbPSCdZ4LP6%2bAEBtA%2boOQ9bEdr0LwbQknqjd8ZkdutBdn9EfnS7KP0mjYldAI%2beekBQOHijrFSlmloMONrdbsYj0gkM8JxRezYG%2bOZCHbRTDLRIwMbPjjZg%2bwfZyWMNVT5vDlI9jb7L1q2hDWbpGk3oVprXKll6%2b3dfTwuVsKRJmpDfQz65oaKObxotc4i5dUVpyfGwIe79JAVMT5mYyb50Wv9zSxzOdLRxkOP8PXODIYc27JZQbUNqyhjgbaIXK9TN%2b8Lp4sm35Sy19d3QKo%2bAADZgAACHgZT5b/yvk0WAEbqZFBuQO%2bZgkYIhhbykgbQiB4h%2bbeIuQIFd%2bl4bzD62I9mBppLGjyeni/j4rDbgkkGvyrt/sNrbWkFwtc0DxzFf4ITE8tLY5o1eJ69m9D1vJ/P8xwuO2y/tSK3zuQdKJXagg7w/zx3qGZJ0OMNt%2bES9xkN1MWcS1ErFadoCAd/O0feLxR3V9HxgsEoX/KPV50yOJFEPjCBVhXzoCiZKpQO8uYE0ttOfzAauVkdhcsjD4RoRBfRO1WfoFkidc1wBntxA6lPnFSYX4xbxYyIMo2WAFdHLTa1AGjLcpNksFYR6NJU8cTenlsx6baLJ6%2bAm9VHoWWiTOF4Q18Stj7yeIPzl1k8hDwAKR0VrYx00TjVABuQOWD6VgsMA0vuER4bI1Wo2siPopQz3UdAgsq/frw3kiZB4PPl89kRfvTuaXjwoljSN3I6GeSiVePvFYjCGyFEQBZr8HE2XMB\r\n");
                    ////builder.Append("Content-Length: 197\r\n");
                    //builder.Append("Content-Length: 217\r\n");
                    //builder.Append("Content-Type: multipart/form-data; boundary=8381f8b9-b470-43ce-b23b-f13cf5840014\r\n");
                    //builder.Append("Host: apis.live.net\r\n");
                    //builder.Append("Connection: Keep-Alive\r\n");
                    //builder.Append("Cache-Control: no-cache\r\n");
                    //builder.Append("\r\n");
                    //builder.Append("--8381f8b9-b470-43ce-b23b-f13cf5840014\r\n");
                    ////builder.Append("Content-Length: 9\r\n");
                    //builder.Append("Content-Type: application/octet-stream; charset=UTF-8\r\n");
                    //builder.Append("Content-Disposition: form-data; name=\"file\"; filename=\"hello.txt\"\r\n");
                    ////builder.Append("Content-Type: application/octet-stream\r\n");
                    //builder.Append("\r\n");
                    //builder.Append("xxxxxxxxx\r\n");
                    //builder.Append("--8381f8b9-b470-43ce-b23b-f13cf5840014--\r\n");
                    //requestBytes = Encoding.UTF8.GetBytes(builder.ToString());

                    stream.Write(requestBytes, 0, requestBytes.Length);
                    Console.WriteLine(Encoding.UTF8.GetString(requestBytes));

                    byte[] responseBytes = new byte[1024];
                    int bytesRead;
                    do
                    {
                        // How to prevent multi-byte chars to be splitted into two buffers?
                        bytesRead = stream.Read(responseBytes, 0, responseBytes.Length);
                        string response = Encoding.UTF8.GetString(responseBytes, 0, bytesRead);
                        Console.Write(response);
                    } while (bytesRead > 0);
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
