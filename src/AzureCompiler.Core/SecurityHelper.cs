using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AzureCompiler.Core
{
    internal static class SecurityHelper
    {
        public static String GenerateDecryptionKey()
        {
            RNGCryptoServiceProvider srng = new RNGCryptoServiceProvider();
            return WriteKeyAsHexDigits(GetRandom(srng, 24));
        }
        public static String GenerateValidationKey()
        {
            RNGCryptoServiceProvider srng = new RNGCryptoServiceProvider();
            return WriteKeyAsHexDigits(GetRandom(srng, 64));
        }
        private static Byte[] GetRandom(RNGCryptoServiceProvider srng, int cb)
        {
            byte[] randomData = new byte[cb];
            srng.GetBytes(randomData);
            return randomData;
        }
        private static String WriteKeyAsHexDigits(byte[] key)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte t in key)
                sb.Append(String.Format("{0:X2}", t));
            return sb.ToString();
        }
    }
}
