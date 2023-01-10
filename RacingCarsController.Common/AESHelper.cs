using System.Security.Cryptography;

namespace RacingCarsController.Common
{
    public class AESHelper
    {
        public static byte[] Decrypt(byte[] key, byte[] data)
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 128;
            aes.Padding = PaddingMode.None;
            aes.Mode = CipherMode.ECB;
            using (ICryptoTransform decryptor = aes.CreateDecryptor(key, null))
            {
                byte[] decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);
                decryptor.Dispose();
                return decrypted;
            }
        }

        public static byte[] Encrypt(byte[] key, byte[] data)
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 128;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.ECB;

            using (ICryptoTransform encryptor = aes.CreateEncryptor(key, null))
            {
                byte[] encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
                encryptor.Dispose();
                return encrypted;
            }
        }
    }
}
