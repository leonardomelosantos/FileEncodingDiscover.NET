using System;
using System.IO;
using System.Text;

namespace FileEncodingDiscover
{
    public class FileEncodingDiscoverMethods
    {

        #region Public methods

        /// <summary>
        /// Method return the encoding of the file.
        /// </summary>
        /// <param name="fullPathFile"></param>
        /// <returns></returns>
        public static Encoding GetEncoding(string fullPathFile)
        {
            return GetEncoding(File.ReadAllBytes(fullPathFile));
        }

        /// <summary>
        /// Method return the encoding of the file.
        /// </summary>
        /// <param name="fileBytes"></param>
        /// <returns></returns>
        public static Encoding GetEncoding(byte[] fileBytes)
        {
            Encoding encondingResult = Encoding.Default;
            byte[] buffer = new byte[5];
            if (buffer[0] == 0xff && buffer[1] == 0xfe)
            {
                encondingResult = Encoding.Unicode;
            }
            else if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
            {
                encondingResult = Encoding.UTF32;
            }
            else if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
            {
                encondingResult = Encoding.UTF7;
            }
            else if (buffer[0] == 0xfe && buffer[1] == 0xff)
            {
                encondingResult = Encoding.BigEndianUnicode;
            }
            else if ((buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf) || IsUTF8(fileBytes, fileBytes.Length))
            {
                encondingResult = Encoding.UTF8;
            }

            return encondingResult;
        }

        /// <summary>
        /// Method return the encoding of the file.
        /// </summary>
        /// <param name="contentStream"></param>
        /// <returns></returns>
        public static Encoding GetEncoding(Stream contentStream)
        {
            return GetEncoding(ToByteArray(contentStream));
        }

        #endregion

        #region Internal methods

        private static bool IsUTF8(string fileName)
        {
            using (BufferedStream bufferedStream = new BufferedStream(File.OpenRead(fileName)))
            {
                return IsUTF8(bufferedStream);
            }
        }

        private static bool IsUTF8(Stream stream)
        {
            // Reference: https://utf8checker.codeplex.com

            int count = 4 * 1024;
            byte[] buffer;
            int read;
            while (true)
            {
                buffer = new byte[count];
                stream.Seek(0, SeekOrigin.Begin);
                read = stream.Read(buffer, 0, count);
                if (read < count)
                {
                    break;
                }
                buffer = null;
                count *= 2;
            }
            return IsUTF8(buffer, read);
        }

        private static bool IsUTF8(byte[] buffer, int length)
        {
            // Reference: https://utf8checker.codeplex.com

            int position = 0;
            int bytes = 0;
            while (position < length)
            {
                if (!IsValid(buffer, position, length, ref bytes))
                {
                    return false;
                }
                position += bytes;
            }
            return true;
        }

        private static bool IsValid(byte[] buffer, int position, int length, ref int bytes)
        {
            // Reference: https://utf8checker.codeplex.com

            if (length > buffer.Length)
            {
                throw new ArgumentException("Invalid length");
            }

            if (position > length - 1)
            {
                bytes = 0;
                return true;
            }

            byte ch = buffer[position];

            if (ch <= 0x7F)
            {
                bytes = 1;
                return true;
            }

            if (ch >= 0xc2 && ch <= 0xdf)
            {
                if (position >= length - 2)
                {
                    bytes = 0;
                    return false;
                }
                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 2;
                return true;
            }

            if (ch == 0xe0)
            {
                if (position >= length - 3)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0xa0 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 3;
                return true;
            }


            if (ch >= 0xe1 && ch <= 0xef)
            {
                if (position >= length - 3)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 3;
                return true;
            }

            if (ch == 0xf0)
            {
                if (position >= length - 4)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x90 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 4;
                return true;
            }

            if (ch == 0xf4)
            {
                if (position >= length - 4)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0x8f ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 4;
                return true;
            }

            if (ch >= 0xf1 && ch <= 0xf3)
            {
                if (position >= length - 4)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 4;
                return true;
            }

            return false;
        }

        private static byte[] ToByteArray(Stream stream)
        {
            long num1 = 0L;
            if (stream.CanSeek)
            {
                num1 = stream.Position;
                stream.Position = 0L;
            }
            try
            {
                byte[] buffer = new byte[4096];
                int length = 0;
                int num2;
                while ((num2 = stream.Read(buffer, length, buffer.Length - length)) > 0)
                {
                    length += num2;
                    if (length == buffer.Length)
                    {
                        int num3 = stream.ReadByte();
                        if (num3 != -1)
                        {
                            byte[] numArray = new byte[buffer.Length * 2];
                            Buffer.BlockCopy((Array)buffer, 0, (Array)numArray, 0, buffer.Length);
                            Buffer.SetByte((Array)numArray, length, (byte)num3);
                            buffer = numArray;
                            ++length;
                        }
                    }
                }
                byte[] numArray1 = buffer;
                if (buffer.Length != length)
                {
                    numArray1 = new byte[length];
                    Buffer.BlockCopy((Array)buffer, 0, (Array)numArray1, 0, length);
                }
                return numArray1;
            }
            finally
            {
                if (stream.CanSeek)
                    stream.Position = num1;
            }
        }

        #endregion

    }
}
