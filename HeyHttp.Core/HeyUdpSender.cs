using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace HeyHttp.Core
{
    public class HeyUdpSender
    {
        public static void Start(HeyUdpSenderSettings settings)
        {
            try
            {
                UdpClient udpClient = new UdpClient(settings.Hostname, settings.Port);

                byte[] messageBytes = Encoding.UTF8.GetBytes(settings.Message);
                udpClient.Send(messageBytes, messageBytes.Length);

                Console.WriteLine("Message sent: {0}", settings.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
