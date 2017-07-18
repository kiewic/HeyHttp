using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HeyHttp.Core
{
    enum HttpResponseOption
    {
        None,
        Slow,
        Pause
    }

    class HeyHttpServerThread
    {
        private const int MaxChunckSize = 100;
        private HeyLogger logger;
        private RemoveConnectionDelegate removeConnection;
        private Socket clientSocket;
        private Stream networkStream;
        private HeyHttpRequest request;
        private HttpLogFile logFile = null;
        private bool retryReceivedBefore = false;
        private bool keepAlive = true;
        private int connectionId;

        // Stats.
        private static int connectionsCount = 0;
        private int requestsCount;

        public HeyHttpServerThread(HeyLogger logger, Socket tcpClient, RemoveConnectionDelegate removeConnection)
        {
            this.clientSocket = tcpClient;
            this.removeConnection = removeConnection;
            this.logger = logger;
        }

        public Task Start()
        {
            return Task.Run(() =>
            {
                try
                {
                    // Set connection id and increment the connections count.
                    connectionId = connectionsCount++;

                    logger.WriteTransportLine(String.Format(
                        "Connected from {0} (counter: {1})\r\n",
                        clientSocket.RemoteEndPoint,
                        connectionId));

                    // This makes whatever you write in the stream is sent immediately.
                    clientSocket.NoDelay = true;

                    using (networkStream = new NetworkStream(clientSocket))
                    {
                        AcceptCore();
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteErrorLine(ex.Message);
                }
                finally
                {
                    logger.WriteTransportLine(String.Format(
                        "Disconnected ({0}). Served {1} requests.",
                        connectionId,
                        requestsCount));
                    clientSocket.Close();

                    if (removeConnection != null)
                    {
                        removeConnection(this);
                    }
                }
            });
        }

        public void Accept(Stream stream)
        {
            networkStream = stream;
            AcceptCore();
        }

        private void AcceptCore()
        {
            requestsCount = 0;
            keepAlive = true;
            while (keepAlive)
            {
                using (var requestTelemetry = GlobalSettings.HttpInsights.StartRequestTelemetry(String.Empty))
                {
                    using (logFile = new HttpLogFile(clientSocket.RemoteEndPoint, requestsCount + 1))
                    {
                        ReadRequestHeaders();
                        ReadRequestContent();

                        keepAlive = request.IsKeepAlive;

                        HeyHttpResponse response = new HeyHttpResponse(logger)
                        {
                            Status = "200 OK",
                            IsKeepAlive = request.IsKeepAlive
                        };
                        SendResponse(response);
                        requestsCount++;

                        requestTelemetry.Url = request.Url;
                        requestTelemetry.ResponseCode = response.StatusCode;
                    }
                }
            }
        }

        private void ReadRequestHeaders()
        {
            using (FileStream logFileStream = File.OpenWrite(logFile.FullName))
            {
                request = new HeyHttpRequest(logger);
                request.ReadHeaders(networkStream, logFileStream);
            }
        }

        private void ReadRequestContent()
        {
            using (FileStream logFileStream = File.OpenWrite(logFile.FullName))
            {
                if (request.GetHeader("Transfer-Encoding") == "chunked")
                {
                    ReadChunkedRequestContent(logFileStream);
                }
                else
                {
                    ReadNormalRequestContent(logFileStream);
                }
            }
        }



        private void ReadChunkedRequestContent(FileStream requestContentFile)
        {
            long chunkSize = ReadChunkSize();
            while (chunkSize > 0)
            {
                long totalBytesRead = 0;
                while (totalBytesRead < chunkSize)
                {
                    byte[] buffer = new byte[chunkSize - totalBytesRead];
                    int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                    requestContentFile.Write(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                }

                logger.WriteBodyLine(
                    String.Format("Chunk of {0,9} bytes read", chunkSize));

                ReadUntilCrLf();

                chunkSize = ReadChunkSize();
            }

            // Chunked transfer coding ends with a CRLF.
            ReadUntilCrLf();
        }

        private long ReadChunkSize()
        {
            long chunkSize = 0;

            while (true)
            {
                int c = networkStream.ReadByte();
                Utils.CheckByteRead(c);

                if ((c >= '0' && c <= '9'))
                {
                    chunkSize *= 16;
                    chunkSize += (long)(c - '0');
                }
                else if ((c >= 'a' && c <= 'f'))
                {
                    chunkSize *= 16;
                    chunkSize += (long)(c - 'a') + 10;
                }
                else if ((c >= 'A' && c <= 'F'))
                {
                    chunkSize *= 16;
                    chunkSize += (long)(c - 'A') + 10;
                }
                else
                {
                    ReadUntilCrLf(c);
                    break;
                }
            }

            return chunkSize;
        }

        private void ReadUntilCrLf()
        {
            int c = networkStream.ReadByte();
            Utils.CheckByteRead(c);
            ReadUntilCrLf(c);
        }

        private void ReadUntilCrLf(int c)
        {
            while (c != '\r')
            {
                c = networkStream.ReadByte();
                Utils.CheckByteRead(c);
            }

            c = networkStream.ReadByte();
            Utils.CheckByteRead(c);
            if (c != '\n')
            {
                throw new Exception("Line feed expected.");
            }
        }

        private void ReadNormalRequestContent(FileStream requestContentFile)
        {
            byte[] buffer = new byte[1024 * 4];
            long totalBytesRead = 0;
            int bytesRead = 0;

            while (totalBytesRead < request.ContentLength)
            {
                bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                totalBytesRead += bytesRead;

                if (bytesRead == 0)
                {
                    // End of the stream has been reached.
                    throw new Exception("End of the stream has been reached.");
                }

                logger.WriteBodyLine(
                    Encoding.UTF8.GetString(buffer, 0, bytesRead));
                logger.WriteBodyLine(
                    String.Format("{0,9} of {1,9} bytes read.", totalBytesRead, request.ContentLength));
                requestContentFile.Write(buffer, 0, bytesRead);
            }

            logger.WriteBodyLine("");
        }

        private string GetMediaType(string fileName)
        {
            string extension = Path.GetExtension(fileName);

            // Reference: http://en.wikipedia.org/wiki/Internet_media_type
            switch (extension)
            {
                case ".png":
                    return "image/png";
                case ".jpg":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                case ".mp4":
                    return "video/mp4";
                case ".xml":
                    return "text/xml; charset=UTF-8";
                case ".css":
                    return "text/css; charset=UTF-8";
                case ".html":
                    return "text/html; charset=UTF-8";
                case ".atom":
                    return "application/atom+xml;type=entry;charset=\"utf-8\""; // Charset with quotes.
                case ".rss":
                    return "application/rss+xml; charset=UTF-8";
                case ".json":
                    return "application/json; charset=UTF-8";
                default:
                    return "text/plain; charset=UTF-8";
            }
        }

        private void SendResponse(HeyHttpResponse response)
        {
            try
            {
                // Set default content.
                response.ContentStream = CreateDefaultResponseStream();

                // Do authentication.
                HeyHttpAuthentication.AddAuthenticateHeaderIfNeeded(request, response);

                // Custom headers.
                if (request.QueryStringHasTrueValue("custom"))
                {
                    response.Headers.Add("X-Header: Value1");
                    response.Headers.Add("X-Header: Value2");
                    response.Headers.Add("X-Header: Value3");
                }

                // Show the 'Save As' dialog.
                string filename;
                if (request.QueryStringHas("filename", out filename))
                {
                    response.Headers.Add(String.Format("Content-Disposition: Attachment; filename={0}", filename));
                }

                // 201 Created for AtomPub.
                if (request.QueryStringHasTrueValue("create"))
                {
                    response.Status = "201 Created";
                    response.Headers.Add("Location: http://heyhttp.org/Data/first-post.atom");
                    response.Headers.Add("Content-Location: http://heyhttp.org/Data/first-post.atom");
                    response.Headers.Add("ETag: \"e180ee84f0671b1\"");
                }

                // 301 Moved Permanently.
                string redirect;
                if (request.QueryStringHas("redirect", out redirect))
                {
                    response.Status = "301 Moved Permanently";

                    response.Headers.Add("Location: " + redirect);

                    // Do not send any data back.
                    request.Path = "";
                }

                // 503 Service Unavailable, retry after N seconds.
                if (request.QueryStringHasTrueValue("retry"))
                {
                    if (!retryReceivedBefore)
                    {
                        response.Status = "503 Service Unavailable";
                        response.Headers.Add("Retry-After: 5");
                        response.ContentStream = GetStringStream(String.Empty);
                    }
                    retryReceivedBefore = !retryReceivedBefore;
                }

                // Set cache headers.
                if (request.QueryStringHasTrueValue("cache"))
                {
                    response.Headers.Add("Cache-Control: max-age=3600"); // One hour (60 minutes * 60 seconds)

                    //additionalHeaders.Add("Expires: " + DateTime.UtcNow.AddHours(24).ToString("R"));
                }
                else if (request.QueryStringHasTrueValue("nocache"))
                {
                    response.Headers.Add("Cache-Control: no-cache"); // HTTP 1.1
                }

                // Set cookie header.
                if (request.QueryStringHasTrueValue("setcookie"))
                {
                    response.Headers.Add("Set-Cookie: sessionTestCookie=X");
                    response.Headers.Add("Set-Cookie: persistentTestCookie=Y; expires=Wednesday, 09-Nov-2020 23:12:40 GMT");
                    response.Headers.Add("Set-Cookie: httpOnlyTestCookie=X; expires=Wednesday, 09-Nov-2020 23:12:40 GMT; HttpOnly");
                    response.Headers.Add("Set-Cookie: subdomainTestCookie=ghi; expires=Wednesday, 09-Nov-2020 23:12:40 GMT; domain=foo.heyhttp.org");
                    response.Headers.Add("Set-Cookie: slashEndingCookie=b; expires=Wednesday, 09-Nov-2020 23:12:40 GMT; domain=heyhttp.org; path=/foo/");
                    response.Headers.Add("Set-Cookie: nonSlashEndingCookie=a; expires=Wednesday, 09-Nov-2020 23:12:40 GMT; domain=heyhttp.org; path=/foo");
                }

                // Return an specific HTTP status.
                int requestedStatus;
                if (request.QueryStringHas("status", out requestedStatus))
                {
                    response.Status = String.Format("{0} Foo Foo Bar", requestedStatus);
                }

                // Set arbitrary header.
                string headerName;
                string headerValue;
                if (request.QueryStringHas("name", out headerName) && request.QueryStringHas("value", out headerValue))
                {
                    response.Headers.Add(String.Format("{0}: {1}", headerName, headerValue));
                }

                // Introduce a long delay.
                int delay;
                if (request.QueryStringHas("delay", out delay))
                {
                    Thread.Sleep(delay);
                }

                // Get trace content stream.
                if (request.QueryStringHasTrueValue("trace"))
                {
                    response.ContentStream = GetTraceStream();

                    // Do not send any data back.
                    request.Path = "";
                }


                // Get file.
                if (!String.IsNullOrEmpty(request.Path) &&
                    !String.IsNullOrEmpty(response.Status) &&
                    request.Path != "/" &&
                    !response.Status.StartsWith("404"))
                {
                    try
                    {
                        response.ContentStream = GetFileStream();
                    }
                    catch (FileNotFoundException ex)
                    {
                        response.Status = "404 Not Found";
                        response.ContentStream = GetStringStream(ex.Message);
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        response.Status = "404 Not Found";
                        response.ContentStream = GetStringStream(ex.Message);
                    }
                }

                // Create a response of the length given.
                long sizeInBytes;
                if (request.QueryStringHas("length", out sizeInBytes))
                {
                    response.ContentStream = CreateBigStream(sizeInBytes);
                }

                // This check must be done after the response coontent has been selected.
                string eTag;
                bool useETag = request.QueryStringHas("etag", out eTag);
                if (useETag)
                {
                    //// 304 Not Modified with ETag.
                    //if (request.GetHeader("If-None-Match") == eTag)
                    //{
                    //    response.Status = "304 Not Modified";
                    //    response.ContentStream = GetStringStream(String.Empty);
                    //}
                    //else
                    //{
                    //}

                    // This 'if' is to manually change the flow when debugging.
                    if (DateTime.Now.Second == -1)
                    {
                        eTag = String.Empty;
                        response.Status = "404 NOT FOUND";
                        response.ContentStream = GetStringStream("Oops!");
                    }
                    else
                    {
                        response.Headers.Add(String.Format("ETag: \"{0}\"", eTag));
                        response.Headers.Add("Accept-Ranges: bytes");
                    }
                }

                bool useLastModified = request.QueryStringHasTrueValue("lastModified");
                if (useLastModified)
                {
                    response.Headers.Add("Last-Modified: Thu, 21 Aug 2014 21:34:57 GMT");
                    response.Headers.Add("Accept-Ranges: bytes");
                }

                HttpRangeHeader rangeHeader = request.GetRange();
                if (rangeHeader != null && (useETag || useLastModified))
                {
                    response.Status = "206 Partial Content";
                    response.Headers.Add(rangeHeader.GetContentRange(response.ContentStream.Length));
                    response.FirstPosition = rangeHeader.FirstPosition;
                    response.LastPosition = rangeHeader.LastPosition;
                }

                ForkSendResponse(response);
            }
            finally
            {
                // 'finally' block is executed even if a 'try' or 'catch' block contains a 'return'.
                if (response.ContentStream != null)
                {
                    response.ContentStream.Dispose();
                }
            }
        }

        private Stream CreateDefaultResponseStream()
        {
            // Option 1: SSL certificate info.
            string responseContent;
            if (SslClientCertificateInfo.TryGet(networkStream.GetHashCode(), out responseContent))
            {
                return GetStringStream(responseContent);
            }

            // TODO: Check is file exists to avoid having to catch an exception.
            try
            {
                // Option 2: README.md file content.
                return GetFileStream("README.md");
            }
            catch (FileNotFoundException)
            {
                // Option 3: Default Unicode string.
                return GetStringStream(String.Format("¿ñoño? {0}", DateTime.Now));
            }
        }

        private void ForkSendResponse(HeyHttpResponse response)
        {
            // Fork gzip response.
            if (request.QueryStringHasTrueValue("gzip"))
            {
                SendGZipResponse(response);
                return;
            }

            // Fork slow response.
            int delayInMilliseconds;
            if (request.QueryStringHas("slow", out delayInMilliseconds))
            {
                SendSlowResponse(response, delayInMilliseconds, HttpResponseOption.Slow);
                return;
            }

            // Fork slow response with ReadLine pauses.
            if (request.QueryStringHasTrueValue("pause"))
            {
                SendSlowResponse(response, 0, HttpResponseOption.Pause);
                return;
            }

            // Fork chunked response.
            if (request.QueryStringHasTrueValue("chunked"))
            {
                SendChunkedResponse(response);
                return;
            }

            // Send normal response.
            SendSlowResponse(response, 0, HttpResponseOption.None);
        }

        private Stream GetTraceStream()
        {
            var x = request.ConcatenateHeaders();
            return GetStringStream(x);
        }

        private Stream GetFileStream()
        {
            return GetFileStream(request.Path);
        }

        private Stream GetFileStream(string path)
        {
            path = MakeRelativePath(path);
            path = Path.Combine("wwwroot", path);

            CheckItIsSafePath(path);

            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        // TODO: Unit test this.
        private string MakeRelativePath(string path)
        {
            // Remove driver letters.
                int indexColon = path.LastIndexOf(":");
            if (indexColon != -1)
            {
                if (path.Length > indexColon + 1)
                {
                    path = path.Substring(indexColon + 1);
                }
                else
                {
                    path = String.Empty;
                }
            }

            // Remove leading slashes.
            while (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            // Remove leading back-slashes.
            while (path.StartsWith("\\"))
            {
                path = path.Substring(1);
            }

            return path;
        }

        // Check the serving file is within the project folders,
        // otherwise we may be exposing confidential data.
        private void CheckItIsSafePath(string path)
        {
            FileInfo fileInfo = new FileInfo(path);

            string wwwrootPath = Path.Combine(Utils.GetApplicationDirectory(), "wwwroot");

            if (!fileInfo.FullName.StartsWith(wwwrootPath))
            {
                throw new ArgumentException(String.Format(
                    "{0} is not within {1}",
                    fileInfo.FullName,
                    wwwrootPath),
                    "path");
            }
        }

        private Stream GetStringStream(string contentString)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(contentString));
        }

        private Stream CreateBigStream(long sizeInBytes)
        {
            return new FakeStream(sizeInBytes);
        }

        private void SendSlowResponse(
            HeyHttpResponse response,
            int delayInMilliseconds,
            HttpResponseOption option)
        {
            Stream stream = response.ContentStream;

            long idleLength;
            if (!request.QueryStringHas("idleLength", out idleLength))
            {
                idleLength = -1;
            }

            // Calculate positions.

            long totalBytesRead= 0;
            long firstPosition = 0;
            if (response.FirstPosition > 0)
            {
                firstPosition = response.FirstPosition;
                stream.Position = firstPosition;
            }

            long length = response.ContentStream.Length;
            if (response.LastPosition > 0)
            {
                length = response.LastPosition - firstPosition + 1;
            }

            // Send headers.
            response.Headers.Add("Content-Length: " + length);
            response.Headers.Add("Content-Type: " + GetMediaType(request.Path));
            response.CopyHeadersTo(networkStream);

            // Server must not send a message body when using HEAD method (RFC 7231 4.3.2 HEAD).
            if (request.IsHeadMethod)
            {
                return;
            }

            // Calulate buffer size.
            int bufferLength;
            if (!request.QueryStringHas("bufferLength", out bufferLength))
            {
                bufferLength = 1000000; // 1 MB
            }
            byte[] buffer = new byte[bufferLength];

            while (totalBytesRead < length)
            {
                long remainingBytes = length - totalBytesRead;
                int loopLength = (int)Math.Min(bufferLength, remainingBytes);
                int localBytesRead = stream.Read(buffer, 0, loopLength);

                if (option == HttpResponseOption.Pause)
                {
                    BlockThreadUntilEnterIsPressed();
                }

                if (option == HttpResponseOption.Slow)
                {
                    Thread.Sleep(delayInMilliseconds);
                }

                networkStream.Write(buffer, 0, localBytesRead);
                networkStream.Flush();

                totalBytesRead += localBytesRead;
                logger.WriteBodyLine(String.Format(
                    "{0,13:N0} of {1,13:N0} bytes sent.",
                    totalBytesRead,
                    length));

                if (idleLength >= 0 && totalBytesRead >= idleLength)
                {
                    BlockThreadUntilClientIsDisconnected();
                }
            }

            logger.WriteBodyLine("Response completed!\r\n");
        }

        private void BlockThreadUntilEnterIsPressed()
        {
            logger.WriteTransportLine("Press ENTER to continue ...");
            FlushConsole();
            Console.ReadLine();
        }

        private void FlushConsole()
        {
            while (Console.KeyAvailable)
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
            }
        }

        private void BlockThreadUntilClientIsDisconnected()
        {
            logger.WriteTransportLine("Idle ...");
            while (clientSocket.Connected)
            {
                Thread.Sleep(1000);
                int bytesSent = clientSocket.Send(new byte[] { });
            }
        }

        private void SendChunkedResponse(HeyHttpResponse response)
        {
            Stream stream = response.ContentStream;

            // Send headers.
            response.Headers.Add("Transfer-Encoding: chunked");
            response.Headers.Add("Trailer: \"Foo\"");
            response.Headers.Add("Content-Type: " + GetMediaType(request.Path));
            response.CopyHeadersTo(networkStream);

            // Send a random amount of bytes.
            Random random = new Random();

            int bytesRead = 0;
            while (bytesRead < stream.Length)
            {
                byte[] buffer;
                int chunkSize = random.Next(1, MaxChunckSize);

                if (stream.Length - bytesRead < chunkSize)
                {
                    chunkSize = (int)stream.Length - bytesRead;
                }

                // Send chunck size and CRLF.
                string chunkSizeString = String.Format("{0:X}\r\n", chunkSize);
                buffer = Encoding.ASCII.GetBytes(chunkSizeString);
                networkStream.Write(buffer, 0, buffer.Length);

                // Send chunck.
                buffer = new byte[chunkSize];
                int localBytesRead = stream.Read(buffer, 0, buffer.Length);
                networkStream.Write(buffer, 0, localBytesRead);

                // Send CRLF.
                buffer = Encoding.ASCII.GetBytes("\r\n");
                networkStream.Write(buffer, 0, buffer.Length);

                networkStream.Flush();

                bytesRead += localBytesRead;
                logger.WriteBodyLine(
                    String.Format("Chunk of {0,13:N0} bytes, {1,13:N0} of {2,13:N0} bytes sent.",
                        chunkSize,
                        bytesRead,
                        stream.Length));

                Thread.Sleep(100);
            }

            StringBuilder theEnd = new StringBuilder();
            // Add last chunk.
            theEnd.Append("0\r\n");

            // Add trailer.
            theEnd.Append("Foo: Bar\r\n");

            // Add last CRLF.
            theEnd.Append("\r\n");

            byte[] lastChunkBuffer = Encoding.ASCII.GetBytes(theEnd.ToString());
            networkStream.Write(lastChunkBuffer, 0, lastChunkBuffer.Length);
        }

        private void SendGZipResponse(HeyHttpResponse response)
        {
            using (MemoryStream compressedStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                {
                    response.ContentStream.CopyTo(gzipStream);

                    // Not sure why, but gzip stream must be close before getting the data.
                    gzipStream.Close();

                    byte[] bytes = compressedStream.ToArray();

                    // Send response headers.
                    response.Headers.Add("Content-Encoding: gzip");
                    response.Headers.Add("Content-Length: " + bytes.Length);
                    response.Headers.Add("Content-Type: " + GetMediaType(request.Path));
                    response.CopyHeadersTo(networkStream);

                    //compressedStream.CopyTo(networkStream);
                    //networkStream.Flush();

                    // For debugging.
                    networkStream.Write(bytes, 0, bytes.Length);
                    logger.WriteBodyLine(BitConverter.ToString(bytes));
                }
            }
        }

        public void Kill()
        {
            if (clientSocket != null)
            {
                logger.WriteTransportLine("Killing connection!");
                clientSocket.Close();
            }
        }
    }
}
