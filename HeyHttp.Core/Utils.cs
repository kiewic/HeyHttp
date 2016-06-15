using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyHttp.Core
{
    class Utils
    {
        public static void CheckByteRead(int byteRead)
        {
            if (byteRead == -1)
            {
                throw new IOException("End of the stream has been reached.");
            }
        }

        // This method reads the lenght of a message represented by the fisrt 4 bytes,
        // then a UTF-8 message as long as the length given.
        private static string ReadMessage(Stream stream)
        {
            byte[] buffer = new byte[4];
            int bytesRead = stream.Read(buffer, 0, 4);
            if (bytesRead != buffer.Length)
            {
                throw new Exception("Incomplete message.");
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            int length = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[length];
            bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead != buffer.Length)
            {
                throw new Exception("Incomplete message.");
            }

            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        public static string GetApplicationDirectory()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }
    }
}
