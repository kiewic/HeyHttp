using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyHttp.Core
{
    public class HttpRangeHeader
    {
        public long FirstPosition { get; set; }
        public long LastPosition { get; set; }

        public HttpRangeHeader(long first, long last)
        {
            FirstPosition = first;
            LastPosition = last;
        }

        public static bool TryParse(string rangeString, out HttpRangeHeader rangeHeader)
        {
            rangeHeader = null;

            if (String.IsNullOrEmpty(rangeString))
            {
                return false;
            }

            int equalIndex = rangeString.IndexOf('=');
            int dashIndex = rangeString.IndexOf('-');

            // The equal should be before the dash.
            if (dashIndex <= equalIndex)
            {
                return false;
            }

            // There should be something else after the equal.
            if (equalIndex + 1 >= rangeString.Length)
            {
                return false;
            }

            string firstBytePosString = rangeString.Substring(equalIndex + 1, dashIndex - equalIndex - 1);

            string lastBytePosString = String.Empty;
            if (dashIndex + 1 <= rangeString.Length)
            {
                lastBytePosString = rangeString.Substring(dashIndex + 1);
            }

            long last;
            if (!String.IsNullOrEmpty(lastBytePosString))
            {
                if (!Int64.TryParse(lastBytePosString, out last))
                {
                    // Invalid header, stop parsing.
                    return false;
                }
            }
            else
            {
                last = Int64.MaxValue;
            }

            long first = 0;
            if (!String.IsNullOrEmpty(firstBytePosString))
            {
                if (!Int64.TryParse(firstBytePosString, out first))
                {
                    // Invalid header, stop parsing.
                    return false;
                }
            }
            else
            {
                last *= -1;
            }

            rangeHeader = new HttpRangeHeader(first, last);
            return true;
        }

        public string GetContentRange(long length)
        {
            if (length == 0)
            {
                return String.Empty;
            }

            Calculate(length);

            return String.Format("Content-Range: bytes {0}-{1}/{2}", FirstPosition, LastPosition, length);
        }

        private void Calculate(long length)
        {
            if (LastPosition < 0)
            {
                FirstPosition = length - LastPosition;
                LastPosition = FirstPosition + Math.Abs(LastPosition) - 1;
            }
            else
            {
                LastPosition = Math.Min(LastPosition, length - 1);
            }
        }
    }
}
