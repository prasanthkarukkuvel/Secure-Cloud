using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace SecureCloud.Models
{
    public class KeyGenerator
    {        
        public static void SetKeyPair(out string publicKey, out string privateKey)
        {
            try
            {
                RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider(1024, new CspParameters { ProviderType = 1 });

                 publicKey = Convert.ToBase64String(rsaProvider.ExportCspBlob(false));
                 privateKey = Convert.ToBase64String(rsaProvider.ExportCspBlob(true));
            }
            catch
            {
                throw;
            }
        }

        public static string CreateKey()
        {
            RNGCryptoServiceProvider cryptRNG = new RNGCryptoServiceProvider();
            byte[] tokenBuffer = new byte[32];
            cryptRNG.GetBytes(tokenBuffer);

            return Convert.ToBase64String(new Rfc2898DeriveBytes(Convert.ToBase64String(tokenBuffer), 300).GetBytes(32));
        }
    }
}