using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Namespace
{
    static class ByteConverter
    {
        public static int ConvertIntBytes(byte[] byteArray)
        {
            Array.Reverse(byteArray);
            uint value = BitConverter.ToUInt32(byteArray);

            return Convert.ToInt32(value);
        }

        public static string ConvertStringBytes(byte[] byteArray)
        {
            Array.Reverse(byteArray);
            return Encoding.UTF8.GetString(byteArray);
        }

        public static bool ConvertBoolBytes(byte[] byteArray)
        {
            Array.Reverse(byteArray);
            return BitConverter.ToBoolean(byteArray);
        }

        public static BitArray ConvertBitBytes(byte[] byteArray)
        {
            Array.Reverse(byteArray);
            return new(byteArray);
        }
    }
}