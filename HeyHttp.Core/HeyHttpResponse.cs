using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HeyHttp.Core
{
    public class HeyHttpResponse
    {
        private HeyLogger logger;
        private Stream contentStream;

        public HeyHttpResponse(HeyLogger logger)
        {
            this.logger = logger;
            Headers = new List<string>();
            Version = "HTTP/1.1";
        }

        public string Version
        {
            get;
            set;
        }

        public string Status
        {
            get;
            set;
        }

        public string StatusCode
        {
            get
            {
                var status = Status;
                if (String.IsNullOrEmpty(status))
                {
                    return String.Empty;
                }
                return status.Split(new char[] { ' ' }, 2)[0];
            }
        }

        public List<string> Headers
        {
            get;
            private set;
        }

        public bool IsKeepAlive
        {
            get;
            set;
        }

        public long FirstPosition
        {
            get;
            set;
        }

        public long LastPosition
        {
            get;
            set;
        }

        public Stream ContentStream
        {
            get
            {
                return contentStream;
            }
            set
            {
                // We do not want to dispose the underlying stream when a GZipStream is assigned.
                if (contentStream != null && !(contentStream is GZipStream))
                {
                    contentStream.Dispose();
                }
                contentStream = value;
            }
        }

        internal void CopyHeadersTo(Stream outputStream)
        {
            // Prepare response headers.
            List<string> responseHeaders = new List<string>();

            responseHeaders.Add(Version + " " + Status);

            // Copy response headers to final set of headers.
            responseHeaders.AddRange(Headers);

            responseHeaders.Add("Date: " + DateTime.UtcNow.ToString("R"));
            if (IsKeepAlive)
            {
                responseHeaders.Add("Connection: Keep-Alive");
            }

            // Convert headers to bytes.
            string headersString = String.Join("\r\n", responseHeaders) + "\r\n\r\n";
            byte[] headersBytes = Encoding.ASCII.GetBytes(headersString);

            // Send headers.
            outputStream.Write(headersBytes, 0, headersBytes.Length);
            outputStream.Flush();

            // For debugging.
            logger.WriteHeaders(Encoding.ASCII.GetString(headersBytes));
        }
    }
}
