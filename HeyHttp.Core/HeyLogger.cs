using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyHttp.Core
{
    public class HeyLogger
    {
        public bool IsBodyLogEnabled
        {
            get;
            set;
        }

        protected bool IsHeadersLogEnabled
        {
            get
            {
                return true;
            }
        }

        protected bool IsErrorLogEnabled
        {
            get
            {
                return true;
            }
        }

        protected bool IsTransportLogEnabled
        {
            get
            {
                return true;
            }
        }

        [Obsolete("We cannot filter logs using this method.")]
        public virtual void WriteLine(string value)
        {
            Debug.WriteLine(value);
        }

        public virtual void WriteErrorLine(string value)
        {
            if (IsErrorLogEnabled)
            {
                Debug.WriteLine(value);
            }
        }

        public virtual void WriteTransportLine(string value)
        {
            if (IsTransportLogEnabled)
            {
                Debug.WriteLine(value);
            }
        }

        public virtual void WriteHeaders(string value)
        {
            if (IsHeadersLogEnabled)
            {
                Debug.Write(value);
            }
        }

        public virtual void WriteHeaders(char c)
        {
            if (IsHeadersLogEnabled)
            {
                Debug.Write(c);
            }
        }

        public virtual void WriteHeadersLine(string value)
        {
            if (IsHeadersLogEnabled)
            {
                Debug.Write(value);
            }
        }

        public virtual void WriteBodyLine(string value)
        {
            if (IsBodyLogEnabled)
            {
                Debug.WriteLine(value);
            }
        }
    }
}
