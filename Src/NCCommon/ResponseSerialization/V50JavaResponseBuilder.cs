using System.Text;

namespace Alachisoft.NCache.Common.ResponseSerialization
{
    internal class V50JavaResponseBuilder: V50ResponseBuilder
    {

        public override byte[] GetRequestIdBuffer(long requestId)
        {
            return ConvertToStringByte(requestId.ToString(), ValSizeHolderBytesCount);
        }

        public override byte[] GetResponseTypeBuffer(short responseType)
        {
            return ConvertToStringByte(responseType.ToString(), ValTypeHolderBytesCount);
        }

        private byte[] ConvertToStringByte(string value, int size)
        {
            byte[] resultBytes = new byte[size];

            var bytearray = Encoding.UTF8.GetBytes(value.ToString());
            for (int i = 0; i < bytearray.Length; i++)
            {
                resultBytes[i] = bytearray[i];
            }
            return resultBytes;
        }
    }

}
