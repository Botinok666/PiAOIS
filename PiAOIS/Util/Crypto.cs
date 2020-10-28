using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PiAOIS.Util
{
    public static class Crypto
    {
        private const int padding = 16;
        private const int _iterations = 2;
        private static readonly Random random = new Random();
        private static readonly string _pass = "milwakee";
        private static readonly string _hash = "SHA256";
        private static readonly byte[] _iv = Encoding.ASCII.GetBytes("aselrias38490a32");
        private static readonly byte[] _salt = Encoding.ASCII.GetBytes("8947az34awl34kjq");
        public static string Encrypt(string value)
        {
            int spaces = value.Length % padding; //How much spaces do we need to pad
            int padLeftWidth = spaces - random.Next(spaces) + value.Length;
            int padRightWidth = spaces + value.Length;
            value = value
                .PadLeft(padLeftWidth)
                .PadRight(padRightWidth);
            byte[] valueBytes = Encoding.Unicode.GetBytes(value);
            using (var cipher = new AesCng())
            {
                PasswordDeriveBytes _passwordBytes =
                    new PasswordDeriveBytes(_pass, _salt, _hash, _iterations);
                cipher.Mode = CipherMode.CBC;
                cipher.IV = _iv;
                cipher.Key = _passwordBytes.GetBytes(cipher.KeySize / 8);
                var encryptor = cipher.CreateEncryptor();
                return Convert.ToBase64String(encryptor.TransformFinalBlock(valueBytes, 0, valueBytes.Length));
            }
        }

        public static bool TryDecryptFloat(string password, string value, out float result)
        { 
            byte[] valueBytes = Convert.FromBase64String(value);
            using (var cipher = new AesCng())
            {
                PasswordDeriveBytes _passwordBytes = 
                    new PasswordDeriveBytes(password, _salt, _hash, _iterations);
                cipher.Mode = CipherMode.CBC;
                cipher.IV = _iv;
                cipher.Key = _passwordBytes.GetBytes(cipher.KeySize / 8);
                try
                {
                    var decryptor = cipher.CreateDecryptor();
                    string vs = Encoding.Unicode.GetString
                        (decryptor.TransformFinalBlock(valueBytes, 0, valueBytes.Length));
                    return float.TryParse(vs, out result);
                }
                catch (Exception)
                {
                    result = 0;
                    return false;
                }
            }
        }
    }
}
