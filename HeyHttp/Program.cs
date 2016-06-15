using HeyHttp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace HeyHttp
{
    class Program
    {
        static void Main(string[] args)
        {
            var argsList = args.ToList();
            var loggerFactory = new ColorLoggerFactory();

            if (argsList.Contains("ssl") && argsList.Contains("server"))
            {
                HeySslServer.Start(SettingsFactory.GetHeySslServerSettings(args, ApplicationLayerProtocol.None));
            }
            if (argsList.Contains("ssl") && argsList.Contains("client"))
            {
                HeySslClient.Start(SettingsFactory.GetHeySslClientSettings(args));
            }
            else if (argsList.Contains("tcp") && argsList.Contains("server"))
            {
                HeyTcpServer.Start(SettingsFactory.GetHeyTcpServerSettings(args));
            }
            else if (argsList.Contains("tcp") && argsList.Contains("client"))
            {
                HeyTcpClient.Start(SettingsFactory.GetHeyTcpClientSettings(args));
            }
            else if (argsList.Contains("udp") && argsList.Contains("receiver"))
            {
                HeyUdpReceiver.Start(SettingsFactory.GetHeyUdpReceiverSettings(args));
            }
            else if (argsList.Contains("udp") && argsList.Contains("sender"))
            {
                HeyUdpSender.Start(SettingsFactory.GetHeyUdpSenderSettings(args));
            }
            else if (argsList.Contains("http") && argsList.Contains("server"))
            {
                HeyHttpServer.Start(SettingsFactory.GetHeyHttpServerSettings(args, loggerFactory));
            }
            else if (argsList.Contains("http") && argsList.Contains("client"))
            {
                HeyHttpClient.Start(SettingsFactory.GetHeyHttpClientSettings(args));
            }
            else if (argsList.Contains("https") && argsList.Contains("server"))
            {
                HeySslServer.Start(SettingsFactory.GetHeySslServerSettings(args, ApplicationLayerProtocol.Http));
            }
            else if (argsList.Contains("ws") && argsList.Contains("server"))
            {
                HeyWebSocketServer.Start(SettingsFactory.GetHeyWebSocketServerSettings(args));
            }
            else if (argsList.Contains("wss") && argsList.Contains("server"))
            {
                var settings = SettingsFactory.GetHeySslServerSettings(args, ApplicationLayerProtocol.Ws);
                HeySslServer.Start(settings);
            }
            else if (argsList.Contains("proxy") && argsList.Contains("server"))
            {
                HeyProxyServer.Start(SettingsFactory.GetHeyProxyServerSettings(args));
            }
            else if (argsList.Contains("tcpchat") && argsList.Contains("server"))
            {
                HeyTcpChatServer.Start();
            }
            else
            {
                PrintHelp();
            }
        }

        private static void PrintHelp()
        {
            string processName = AppDomain.CurrentDomain.FriendlyName;
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            Console.WriteLine("No protocol selected.");
            Console.WriteLine("Version: {0}", fileVersionInfo.FileVersion);
            Console.WriteLine("Usage:");
            Console.WriteLine("    {0} http server [-OnlyHeadersLog] [port] -- port {1} by default", processName, HeyHttpServer.DefaultPort);
            Console.WriteLine("    {0} http client -Head [uri]", processName);
            Console.WriteLine("    {0} https server [-ClientCertificate] [-SubjectName \"CN=mywebsite.com\"] [-IssuerName \"CN=RootCA\"] [port] -- port {1} by default", processName, HeySslServer.DefaultPort);
            Console.WriteLine("    {0} ssl server [port] -- {1} by default", processName, HeySslServer.DefaultPort);
            Console.WriteLine("    {0} ssl client [hostname] [port]", processName);
            Console.WriteLine("    {0} ws server [port] -- port {1} by default", processName, HeyWebSocketServer.DefaultPort);
            Console.WriteLine("    {0} wss server [port] -- port {1} by default", processName, HeySslServer.DefaultPort);
            Console.WriteLine("    {0} proxy server [-NoAuthentication] [-Port {1}]", processName, HeyProxyServer.DefaultPort);
            Console.WriteLine("    {0} udp receiver [port]", processName);
            Console.WriteLine("    {0} udp sender [hostname] [port] [message]", processName);
            Console.WriteLine("    {0} tcp server [-Port {1}]", processName, HeyTcpServer.DefaultPort);
            Console.WriteLine("    {0} tcp client [hostname] [port]", processName);
        }
    }
}
