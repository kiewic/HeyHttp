using HeyHttp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyHttp
{
    public class SettingsFactory
    {
        public static HeyHttpServerSettings GetHeyHttpServerSettings(string[] args)
        {
            var argsParser = new ArgsParser(args);
            var settings = new HeyHttpServerSettings();

            argsParser.GetPort(settings, 2, HeyHttpServer.DefaultPort);

            settings.OnlyHeadersLog = argsParser.HasOption("-OnlyHeadersLog");

            return settings;
        }

        public static HeySslServerSettings GetHeySslServerSettings(string[] args, ApplicationLayerProtocol protocol)
        {
            var argsParser = new ArgsParser(args);
            var settings = new HeySslServerSettings();

            settings.Protocol = protocol;

            settings.ClientCertificateRequired = argsParser.HasOption("ClientCertificate");

            string thumbprint;
            if (!argsParser.TryGetOptionValue("Thumbprint", out thumbprint))
            {
                thumbprint = "c1fd2c54ef2f457f14aa40206118dc2f2a580f5f";
            }
            settings.Thumbprint = thumbprint;

            // Parse options first, then unamed arguments.
            argsParser.GetPort(settings, 2, HeySslServer.DefaultPort);

            return settings;
        }

        public static HeyTcpServerSettings GetHeyTcpServerSettings(string[] args)
        {
            ArgsParser argsParser = new ArgsParser(args);
            var settings = new HeyTcpServerSettings();

            int port;
            if (!argsParser.TryGetOptionValue("Port", out port))
            {
                port = HeyTcpServer.DefaultPort;
            }

            return settings;
        }

        public static HeySslClientSettings GetHeySslClientSettings(string[] args)
        {
            ArgsParser argsParser = new ArgsParser(args);
            var settings = new HeySslClientSettings();

            argsParser.GetHostname(settings, 2, "api.stackexchange.com");
            argsParser.GetPort(settings, 3, 80);

            return settings;
        }

        public static HeyHttpClientSettings GetHeyHttpClientSettings(string[] args)
        {
            ArgsParser argsParser = new ArgsParser(args);
            var settings = new HeyHttpClientSettings();

            string uriString = "http://localhost";
            argsParser.TryGetString(2, ref uriString);
            settings.UriString = uriString;

            string method;
            argsParser.TryGetOptionValue("X", out method);
            settings.Method = method;

            string header = null;
            argsParser.TryGetOptionValue("H", out header);
            settings.Headers.Add(header);

            return settings;
        }

        public static HeyProxyServerSettings GetHeyProxyServerSettings(string[] args)
        {
            ArgsParser argsParser = new ArgsParser(args);
            var settings = new HeyProxyServerSettings();

            settings.AuthenticationRequired = true;
            if (argsParser.HasOption("NoAuthentication"))
            {
                settings.AuthenticationRequired = false;
            }

            int port;
            if (!argsParser.TryGetOptionValue("Port", out port))
            {
                port = HeyProxyServer.DefaultPort;
            }
            settings.Port = port;

            return settings;
        }

        public static HeyTcpClientSettings GetHeyTcpClientSettings(string[] args)
        {
            ArgsParser argsParser = new ArgsParser(args);
            var settings = new HeyTcpClientSettings();

            argsParser.GetHostname(settings, 2, "heyhttp.org");
            argsParser.GetPort(settings, 3, HeyTcpClient.DefaultPort);

            return settings;
        }

        public static HeyUdpReceiverSettings GetHeyUdpReceiverSettings(string[] args)
        {
            ArgsParser argsParser = new ArgsParser(args);
            var settings = new HeyUdpReceiverSettings();

            argsParser.GetPort(settings, 2, HeyUdpReceiver.DefaultPort);

            return settings;
        }

        public static HeyUdpSenderSettings GetHeyUdpSenderSettings(string[] args)
        {
            ArgsParser argsParser = new ArgsParser(args);
            var settings = new HeyUdpSenderSettings();

            argsParser.GetHostname(settings, 2, "172.31.239.255");

            argsParser.GetPort(settings, 3, 5143);

            // U+263A is a smiling face.
            string message = "Hello World \u263A";
            argsParser.TryGetString(4, ref message);
            settings.Message = message;

            return settings;
        }

        public static HeyWebSocketServerSettings GetHeyWebSocketServerSettings(string[] args)
        {
            ArgsParser argsParser = new ArgsParser(args);
            var settings = new HeyWebSocketServerSettings();

            argsParser.GetPort(settings, 2, HeyWebSocketServer.DefaultPort);

            return settings;
        }
    }
}
