using HeyHttp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyHttp
{
    public class ColorLogger : HeyLogger
    {
        private static readonly object consoleLock = new object();
        private static readonly ConsoleColor[] colors = new ConsoleColor[]
        {
            ConsoleColor.White,
            ConsoleColor.Green,
            ConsoleColor.Cyan,
            ConsoleColor.Magenta,
            ConsoleColor.Yellow
        };
        private static int colorIndex = 0;
        private ConsoleColor threadColor;

        public ColorLogger() : base()
        {
            threadColor = colors[colorIndex];
            colorIndex = (colorIndex + 1) % colors.Length;
            IsBodyLogEnabled = true;
        }

        [Obsolete("We cannot filter logs using this method.")]
        public override void WriteLine(string value)
        {
            Write(value + "\r\n", threadColor);
        }

        public override void WriteErrorLine(string value)
        {
            if (IsErrorLogEnabled)
            {
                // Notice there is a race condition where the next two logs may NOT end
                // next ot each other.
                Write("Error: ", ConsoleColor.Red);
                Write(value + "\r\n", threadColor);
            }
        }

        public override void WriteTransportLine(string value)
        {
            if (IsTransportLogEnabled)
            {
                Write(value + "\r\n", threadColor);
            }
        }

        public override void WriteHeaders(string value)
        {
            if (IsHeadersLogEnabled)
            {
                Write(value, threadColor);
            }
        }

        public override void WriteHeaders(char c)
        {
            if (IsHeadersLogEnabled)
            {
                Write(c, threadColor);
            }
        }

        public override void WriteHeadersLine(string value)
        {
            if (IsHeadersLogEnabled)
            {
                Write(value, threadColor);
            }
        }

        public override void WriteBodyLine(string value)
        {
            if (IsBodyLogEnabled)
            {
                WriteLine(value);
            }
        }

        private void Write(string value, ConsoleColor newColor)
        {
            lock (consoleLock)
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = newColor;
                Console.Write(value);
                Console.ForegroundColor = originalColor;
            }
        }

        private void Write(char value, ConsoleColor newColor)
        {
            lock (consoleLock)
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = newColor;
                Console.Write(value);
                Console.ForegroundColor = originalColor;
            }
        }
    }
}
