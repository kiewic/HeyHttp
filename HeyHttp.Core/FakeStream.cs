using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace HeyHttp.Core
{
    internal class FakeStream : Stream
    {
        private long length;
        private long position;

        public FakeStream(long length)
        {
            this.length = length;
            position = 0;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override void Flush()
        {
            throw new InvalidOperationException();
        }

        public override long Length
        {
            get
            {
                return length;
            }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long remainingData = length - position;
            if (remainingData < count)
            {
                // There is less data than the  requested amount.
                count = (int)remainingData;
            }

            // Prepare payload.
            string dummyString = "0123456789ABCDEF";
            byte[] dummyBytes = Encoding.UTF8.GetBytes(dummyString);

            int bytesCopied = 0;

            // If the last dummy was truncated, first append the missing characters.
            int lastDummyLength = (int)(position % dummyBytes.Length);
            if (lastDummyLength != 0)
            {
                int missingCharacters = dummyBytes.Length - lastDummyLength;
                int localCount = Math.Min(count, missingCharacters);
                Array.Copy(dummyBytes, lastDummyLength, buffer, offset, localCount);
                count -= localCount;
                offset += localCount;
                bytesCopied += localCount;
            }

            // Append whole dummies.
            int wholeDummiesCount = count / dummyBytes.Length;
            for (int i = 0; i < wholeDummiesCount; i++)
            {
                Array.Copy(dummyBytes, 0, buffer, offset, dummyBytes.Length);
                count -= dummyBytes.Length;
                offset += dummyBytes.Length;
                bytesCopied += dummyBytes.Length;
            }

            if (count >= dummyBytes.Length)
            {
                throw new Exception("'count' cannot be equal or larger than 'dummyBytes.Length'");
            }

            // Truncate the last dummy.
            Array.Copy(dummyBytes, 0, buffer, offset, count);
            bytesCopied += count;

            // Advance the stream position.
            position += bytesCopied;

            return bytesCopied;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            // TODO; Check this is valid.
            if (value >= position)
            {
                length = value;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }
    }
}
