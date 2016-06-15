using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HeyHttp.Core
{
    class HttpLogFile : IDisposable
    {
        public string FullName { get; private set; }

        public HttpLogFile(EndPoint remoteEndPoint, int requestNumber)
        {
            try
            {
                string processDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                DirectoryInfo directoryInfo = new DirectoryInfo(processDir + "\\Logs");
                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }

                string ipString = remoteEndPoint.ToString().Replace(':', '-');

                FullName = String.Format("{0}\\{1}-{2}.txt", directoryInfo.Name, ipString, requestNumber);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                FileInfo fileInfo = new FileInfo(FullName);

                if (fileInfo.Length == 0)
                {
                    fileInfo.Delete();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }
    }
}
