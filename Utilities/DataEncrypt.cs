using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Samico.Utilities
{
    public class DataEncrypt
    {
        private static byte[] sharedkey = { 0x01, 0x03, 0x07, 0x09, 0x11, 0x09, 0x07, 0x15,
            0x02, 0x04, 0x06, 0x08, 0x10, 0x12, 0x10, 0x08,
            0x0B, 0x0D, 0x0B, 0x0D, 0x0B, 0x0D, 0x0B, 0x0D};
        private static byte[] sharedvector = { 0x03, 0x06, 0x09, 0x12, 0x0B, 0x0D, 0x12, 0x06 };
        public String Decrypt(String val)
        {
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            byte[] toDecrypt = Convert.FromBase64String(val);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, tdes.CreateDecryptor(sharedkey, sharedvector), CryptoStreamMode.Write);

            cs.Write(toDecrypt, 0, toDecrypt.Length);
            cs.FlushFinalBlock();
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public String Encrypt(String val)
        {
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            byte[] toEncrypt = Encoding.UTF8.GetBytes(val);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, tdes.CreateEncryptor(sharedkey, sharedvector), CryptoStreamMode.Write);
            cs.Write(toEncrypt, 0, toEncrypt.Length);
            cs.FlushFinalBlock();
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}