using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;

namespace HeyHttp.Core
{
    public class HeyWebSocketServer
    {
        public const int DefaultPort = 80;
        private static HeyLogger logger;
        private static Stream networkStream;
        private static HeyHttpRequest requestHeaders;

        // Members related to events.
        public delegate void OnMessageHandler(object sender, byte[] data);
        public static event OnMessageHandler OnMessage;

        public enum FinType : byte
        {
            Clear = 0x0,
            Set = 0x80 // I.e.: 1 << 7
        }

        public enum OpcodeType : byte {
            Continuation = 0x0,
            Text = 0x1,
            Binary = 0x2,
            ConnectionClose = 0x8,
            Ping = 0x9,
            Pong = 0xA
        }


        public static void Start(HeyWebSocketServerSettings settings)
        {
            HeyWebSocketServer.OnMessage += (sender, data) =>
            {
                Console.WriteLine(data.Length);

                //// Send a ping.
                //WebSocketServerPro.Ping();

                //WebSocketServerPro.Send(new String('x', 0x10000));

                // Non-fragmented message.
                HeyWebSocketServer.Send(FinType.Set, OpcodeType.Text, "Hey!");

                // Send fragmented message.
                HeyWebSocketServer.Send(FinType.Clear, OpcodeType.Text, "Go");
                Thread.Sleep(1000);
                HeyWebSocketServer.Send(FinType.Set, OpcodeType.Continuation, "Hawks!");
            };

            TcpListener tcpListener = new TcpListener(IPAddress.IPv6Any, settings.Port);
            tcpListener.Start();
            Console.WriteLine("Listening on port {0} ...", settings.Port);

            while (true)
            {
                try
                {
                    using (TcpClient tcpClient = tcpListener.AcceptTcpClient())
                    {
                        Console.WriteLine("Connected: {0}", tcpClient.Client.RemoteEndPoint);
                        Console.WriteLine();

                        using (networkStream = tcpClient.GetStream())
                        {
                            logger = new HeyLogger();
                            AcceptCore();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Console.WriteLine();
                Console.WriteLine("Disconnected.");
            }
        }

        public static void Accept(HeyLogger logger, Stream stream)
        {
            HeyWebSocketServer.logger = logger;
            HeyWebSocketServer.networkStream = stream;
            AcceptCore();
        }

        private static void AcceptCore()
        {
            requestHeaders = new HeyHttpRequest(logger);
            requestHeaders.ReadHeaders(networkStream, null);
            Upgrade();
            //ReadRequestBody();
            //ShowRequestDetails();
            //SendResponse("200 OK", new List<string>());
        }

        private static void Upgrade()
        {
            if(requestHeaders.Method != "GET") {
                throw new Exception("HTTP method must be 'GET'.");
            }

            // E.g.: Upgrade: websocket
            if (requestHeaders.GetHeader("Upgrade", "").ToLower() != "websocket")
            {
                throw new Exception("'Upgrade' header must be 'websocket'.");
            }

            // E.g.: Connection: keep-alive, Upgrade
            if (!requestHeaders.GetHeader("Connection", "").ToLower().Contains("upgrade"))
            {
                throw new Exception("'Connection' header must be 'upgrade'.");
            }

            SendResponseHeaders();
            ReadData();
        }

        private static void SendResponseHeaders()
        {
            string key = requestHeaders.GetHeader("Sec-WebSocket-Key", "");
            key += "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

            // Compute SHA1.
            byte[] data = Encoding.ASCII.GetBytes(key);
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] hash = sha1.ComputeHash(data);

            // Represent the  SHA1 hash as a Base64 string.
            string base64Hash = Convert.ToBase64String(hash);

            List<string> responseHeaders = new List<string>();
            responseHeaders.Add("HTTP/1.1 101 Switching Protocols");
            responseHeaders.Add("Upgrade: websocket");
            responseHeaders.Add("Connection: Upgrade");
            responseHeaders.Add("Sec-WebSocket-Location: " + GetLocation());
            //responseHeaders.Add("Sec-WebSocket-Protocol: chat"); // Select one of the client subprotocols.
            responseHeaders.Add("Sec-WebSocket-Accept: " + base64Hash);

            string headersString = String.Join("\r\n", responseHeaders) + "\r\n\r\n";
            byte[] headersBytes = Encoding.UTF8.GetBytes(headersString);

            // Send response.
            networkStream.Write(headersBytes, 0, headersBytes.Length);
        }

        private static string GetLocation()
        {
            string[] hostPieces = requestHeaders.GetHeader("Host").Split(new char[] { ':' });
            string location = "ws://" + hostPieces[0];
            string port = (hostPieces.Length >= 2) ? hostPieces[1] : "80";

            if (port != "80")
            {
                location += ":" + hostPieces;
            }

            return location;
        }

        private static byte fin;
        private static byte opcode = 0;
        private static byte mask;
        private static UInt64 length;
        private static byte[] maskingKey;
        private static UInt16 statusCode;
        private static byte[] reason;
        private static byte[] payload;

        private static void ReadData()
        {
            while (true)
            {
                ReadFrameHeaders();
                ReadFramePayload();
            }
        }

        private static void ReadFrameHeaders()
        {
            int b = networkStream.ReadByte();
            Utils.CheckByteRead(b);
            if (b == -1)
            {
                throw new Exception("It was not possible to read one byte for fin and opcode.");
            }

            fin = (byte)((b & 0x80) != 0 ? 1 : 0);
            opcode = (byte)(b & 0xF);

            b = networkStream.ReadByte();
            Utils.CheckByteRead(b);
            if (b == -1)
            {
                throw new Exception("It was not possible to read one byte for mask and length.");
            }

            mask = (byte)((b & 0x80) != 0 ? 1 : 0);
            length = (byte)(b & 0x7F);

            Console.WriteLine("fin: {0} opcode: {1:X} mask: {2} length: {3}", fin, opcode, mask, length);

            if (length == 126)
            {
                // If length is 126, the following 2 bytes are the payload length.
                ReadFrameLength(2);
            }
            else if (length == 127)
            {
                // If length is 127, the following 8 bytes are the payload length.
                ReadFrameLength(8);
            }

            ReadMaskingKey();
        }

        private static void ReadFrameLength(int sizeInBytes)
        {
            byte[] buffer = new byte[sizeInBytes];
            int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
            if (bytesRead != buffer.Length)
            {
                throw new Exception("It was not possible to read enough bytes for the frame length.");
            }

            Array.Reverse(buffer);
            if (buffer.Length == 2)
            {
                length = BitConverter.ToUInt16(buffer, 0);
            }
            else
            {
                length = BitConverter.ToUInt64(buffer, 0);
            }

            Console.WriteLine("length: {0}", length);
        }

        private static void ReadMaskingKey()
        {
            maskingKey = new byte[4];
            int bytesRead = networkStream.Read(maskingKey, 0, maskingKey.Length);
            if (bytesRead != maskingKey.Length)
            {
                throw new Exception("It was not possible to read enough bytes for the masking key.");
            }

            Console.WriteLine("marking key: {0}", BitConverter.ToString(maskingKey));
        }

        private static void ReadFramePayload()
        {
            if (opcode == (byte)OpcodeType.ConnectionClose && length > 0)
            {
                // Close frame with reason for closing.
                ReadStatusCode();
                ReadReason();
            }
            else
            {
                ReadAndUnmaskPayload();
            }
        }

        private static void ReadStatusCode()
        {
            byte[] buffer = new byte[2];
            int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
            if (bytesRead != buffer.Length)
            {
                throw new Exception("It was not possible to read enough bytes for the status code.");
            }

            statusCode = BitConverter.ToUInt16(buffer, 0);

            Console.WriteLine("status code: {0}", statusCode);
        }

        private static void ReadReason()
        {
            reason = new byte[length - 2];
            int bytesRead = networkStream.Read(reason, 0, reason.Length);
            if (bytesRead != reason.Length)
            {
                throw new Exception("It was not possible to read enough bytes for the reason.");
            }

            Unmask(reason);

            Console.WriteLine("reason: {0}", Encoding.UTF8.GetString(reason));
        }

        private static void ReadAndUnmaskPayload()
        {
            payload = new byte[length];
            int bytesRead = networkStream.Read(payload, 0, payload.Length);
            if (bytesRead != payload.Length)
            {
                throw new Exception("It was not possible to read enough bytes for the payload.");
            }

            Unmask(payload);

            Console.WriteLine("payload: {0}", Encoding.UTF8.GetString(payload));

            if (OnMessage != null)
            {
                OnMessage(null, payload); // TODO: pass a sender
            }
        }

        private static void Unmask(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] ^= maskingKey[i % 4];
            }
        }

        // Suggested opcode: OpcodeType.Text
        public static void Send(FinType fin, OpcodeType opcode, string message)
        {
            SendInternal(fin, opcode, Encoding.UTF8.GetBytes(message));
        }

        public static void Send(byte[] data)
        {
            SendInternal(FinType.Set, OpcodeType.Binary, data);
        }

        public static void Ping()
        {
            SendInternal(FinType.Set, OpcodeType.Ping, Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog."));
        }

        private static void Pong()
        {
            // TODO: implement.
        }

        public static void Close()
        {
            // TODO: align with specification.
            SendInternal(FinType.Set, OpcodeType.ConnectionClose, new byte[0]);
        }

        private static void SendInternal(FinType fin, OpcodeType opcode, byte[] data)
        {
            byte secondByte = 0;
            UInt32 lengthLength = 0;
            if (data.Length > 0xFFFF)
            {
                secondByte = 127;
                lengthLength = 8;
            }
            else if (data.Length >= 126 && data.Length <= 0xFFFF)
            {
                secondByte = 126;
                lengthLength = 2;
            }
            else
            {
                secondByte = (byte)data.Length;
            }

            byte[] headersBuffer = new byte[lengthLength + 2];
            headersBuffer[0] = (byte)((byte)fin | (byte)opcode);
            headersBuffer[1] = secondByte;
            CopyLengthBytes(data.Length, headersBuffer, 2);

            networkStream.Write(headersBuffer, 0, headersBuffer.Length);
            networkStream.Write(data, 0, data.Length);
        }

        private static int CopyLengthBytes(int length, byte[] buffer, int position)
        {
            byte[] lengthBytes = null;
            if (length > 0xFFFF)
            {
                lengthBytes = BitConverter.GetBytes((UInt64)length);
                Debug.Assert(lengthBytes.Length == 8);
            }
            else if (length >= 126 && length <= 0xFFFF)
            {
                lengthBytes = BitConverter.GetBytes((UInt16)length);
                Debug.Assert(lengthBytes.Length == 2);
            }
            else
            {
                return 0;
            }

            Array.Reverse(lengthBytes);
            for (int i = 0; i < lengthBytes.Length; i++)
            {
                buffer[position + i] = lengthBytes[i];
            }

            return lengthBytes.Length;
        }
    }
}
