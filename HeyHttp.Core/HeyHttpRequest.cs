using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace HeyHttp.Core
{
    enum LastChars
    {
        None = 0,
        Cr,
        CrLf,
        CrLfCr,
    }

    public class HeyHttpRequest
    {
        private HeyLogger logger;
        private Dictionary<string, string> headers;

        public string Method
        {
            get;
            set;
        }

        public bool IsHeadMethod
        {
            get
            {
                return Method == "HEAD";
            }
        }

        public string PathAndQuery
        {
            get;
            set;
        }

        public string Path
        {
            get;
            set;
        }

        public string Host
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        }

        public string Version
        {
            get;
            set;
        }

        public long ContentLength
        {
            get;
            set;
        }

        public string Authorization
        {
            get;
            set;
        }

        public string ProxyAuthorization
        {
            get;
            set;
        }

        public Uri Url
        {
            get
            {
                string uriString = String.Format("{0}://{1}:{2}{3}", Port == 80 ? "http" : "https", Host, Port, PathAndQuery);
                Uri result;
                Uri.TryCreate(uriString, UriKind.Absolute, out result);
                return result;
            }
        }

        public bool IsKeepAlive
        {
            get
            {
                // In HTTP 1.1, all connections are considered persistent unless declared otherwise.
                return GetHeader("Connection").ToLower() != "close";
            }
        }

        public HeyHttpRequest(HeyLogger logger)
        {
            this.logger = logger;

            // Use the default port, unless any header specifies a different one.
            Port = 80;

            // Invariant culture is a fake culture based on English, great to write out, for example,
            // stuff into config files so it can be read an written regardless of the culture the user
            // has defined.
            headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            // Initialize with an empty NameValueCollection, so we never have to check if QueryString is null.
            ParseQueryString("");
        }

        public void ReadHeaders(Stream inputStream, Stream outputStream)
        {
            int c;
            StringBuilder builder = new StringBuilder();
            LastChars lastChars = LastChars.None;

            while (true)
            {
                c = inputStream.ReadByte();
                Utils.CheckByteRead(c);

                if (outputStream != null)
                {
                    outputStream.WriteByte((byte)c);
                }

                logger.WriteHeaders((char)c);
                builder.Append((char)c);

                if (c == '\r' && lastChars == LastChars.None)
                {
                    lastChars = LastChars.Cr;
                }
                else if (c == '\n' && lastChars == LastChars.Cr)
                {
                    lastChars = LastChars.CrLf;
                    ParseHeader(builder.ToString());
                    builder.Clear();
                }
                else if (c == '\r' && lastChars == LastChars.CrLf)
                {
                    lastChars = LastChars.CrLfCr;
                }
                else if (c == '\n' && lastChars == LastChars.CrLfCr)
                {
                    break;
                }
                else
                {
                    lastChars = LastChars.None;
                }
            }

            ParseCommonHeaders();
        }

        private void ParseCommonHeaders()
        {
            // Host header.
            string hostString = GetHeader("Host").Trim();
            Host = hostString;
            if (hostString.Contains(':'))
            {
                Host = StripPortFromHost(hostString);
            }

            // Content-Length header.
            string contentLengthString = GetHeader("Content-Length", "0").Trim();
            long localContentLength;
            if (!Int64.TryParse(contentLengthString, out localContentLength))
            {
                throw new Exception("Invalid Content-Length header.");
            }
            ContentLength = localContentLength;

            // Authorization header.
            Authorization = GetHeader("Authorization", "").Trim();
            ProxyAuthorization = GetHeader("Proxy-Authorization", "").Trim();
        }

        private void ParseHeader(string header)
        {
            if (String.IsNullOrEmpty(Method))
            {
                int spaceIndex = header.IndexOf(' ');
                Method = header.Substring(0, spaceIndex);

                int secondSpaceIndex = header.IndexOf(' ', spaceIndex + 1);
                Version = header.Substring(secondSpaceIndex + 1).Trim();

                string uriString = header.Substring(spaceIndex + 1, secondSpaceIndex - spaceIndex - 1);
                ParsePath(uriString);
            }
            else
            {
                string[] substrings = header.Split(new char[] { ':' }, 2);
                SetHeader(substrings[0], substrings[1].Trim());
            }
        }

        // Docs: https://httpwg.github.io/specs/rfc7230.html#request-target
        public void ParsePath(string uriString)
        {
            // E.g.: CONNECT go.microsoft.com:443 HTTP/1.1
            if (uriString.Contains(':') && !uriString.StartsWith("http"))
            {
                Host = StripPortFromHost(uriString);
                Path = "";
            }

            // When proxy is configured, browsers tend to send absolute URIs
            // instead of absolute paths. In this case, remove scheme and host name.
            // E.g.: GET http://www.telerik.com/purchase.aspx HTTP/1.1
            if (uriString.StartsWith("http"))
            {
                Uri uri;
                if (Uri.TryCreate(uriString, UriKind.Absolute, out uri))
                {
                    PathAndQuery = uri.PathAndQuery;
                    SplitPathAndQuery(PathAndQuery);

                    // E.g.: GET http://localhost:1234/ HTTP/1.1
                    if (uri.Port != 80)
                    {
                        Port = uri.Port;
                    }
                }
            }
            else
            {
                // It is a relative URI.

                PathAndQuery = uriString;
                SplitPathAndQuery(PathAndQuery);
            }

            // Remove percent encoding.
            Path = HttpUtility.UrlDecode(Path);
        }

        private void SplitPathAndQuery(string uriString)
        {
            if (uriString.Contains("?"))
            {
                string[] substrings = uriString.Split(new char[] { '?' }, 2);
                Path = substrings[0];
                ParseQueryString(substrings[1]);
            }
            else
            {
                Path = uriString;
            }
        }

        private void ParseQueryString(string query)
        {
            QueryString = HttpUtility.ParseQueryString(query);
        }

        private string StripPortFromHost(string hostString)
        {
            int colonIndex = hostString.IndexOf(':');

            string portString = hostString.Substring(colonIndex + 1);
            int port;
            if (!Int32.TryParse(portString, out port))
            {
                throw new Exception("Invalid Host header.");
            }
            Port = port;

            return hostString.Substring(0, colonIndex);
        }

        public void SetHeader(string key, string value)
        {
            headers[key.Trim()] = value;
        }

        public void SetHeader(string header)
        {
            var substrings = header.Split(new char[] { ':' }, 2);
            SetHeader(substrings[0], substrings[1]);
        }

        public string GetHeader(string key)
        {
            key = key.Trim();
            if (headers.ContainsKey(key))
            {
                return headers[key];
            }
            return "";
        }

        public string GetHeader(string key, string defaultValue)
        {
            key = key.Trim();
            if (headers.ContainsKey(key))
            {
                return headers[key];
            }
            return defaultValue;
        }

        public HttpRangeHeader GetRange()
        {
            string rangeString = GetHeader("Range");

            if (String.IsNullOrEmpty(rangeString))
            {
                return null;
            }

            HttpRangeHeader rangeHeader;
            if (HttpRangeHeader.TryParse(rangeString, out rangeHeader))
            {
                return rangeHeader;
            }

            return null;
        }

        public string ConcatenateHeaders()
        {
            StringBuilder output = new StringBuilder();
            output.Append(Method);
            output.Append(" ");
            output.Append(PathAndQuery);
            output.Append(" ");
            output.Append(Version);
            output.Append("\r\n");
            foreach (var header in headers)
            {
                output.Append(header.Key);
                output.Append(": ");
                output.Append(header.Value);
                output.Append("\r\n");
            }
            output.Append("\r\n");
            return output.ToString();
        }

        #region QueryString stuff.

        public NameValueCollection QueryString
        {
            get;
            private set;
        }

        public bool QueryStringHas(string key, out string value)
        {
            value = QueryString[key];
            return (value != null);
        }

        public bool QueryStringHas(string key, out int value)
        {
            value = 0;
            string valueString = QueryString[key];

            if (String.IsNullOrEmpty(valueString))
            {
                return false;
            }

            return Int32.TryParse(valueString, out value);
        }

        public bool QueryStringHas(string key, out long value)
        {
            value = 0;
            string valueString = QueryString[key];

            if (String.IsNullOrEmpty(valueString))
            {
                return false;
            }

            return Int64.TryParse(valueString, out value);
        }

        public bool QueryStringHasTrueValue(string key)
        {
            string valueString = QueryString[key];

            if (String.IsNullOrEmpty(valueString))
            {
                return false;
            }

            bool valueBool = false;
            if (!Boolean.TryParse(valueString, out valueBool))
            {
                int valueInt;
                if (Int32.TryParse(valueString, out valueInt))
                {
                    if (valueInt != 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            return valueBool;
        }

        #endregion
    }
}
