using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleDB.Util
{
    public static class Crypto
    {
        private const int _iterations = 6666;
        private static readonly string _pass = "Delaware";
        private static readonly Aes _aes = new AesCng() { Mode = CipherMode.CBC };
        private static readonly byte[] _salt = Convert.FromBase64String("vOVH6sdmpNWjRRIqCc7rdxs01lwHzfr3");
        public static void Init()
        {
            _aes.Key = new Rfc2898DeriveBytes(_pass, _salt, _iterations, HashAlgorithmName.SHA256)
                .GetBytes(_aes.KeySize / 8);
        }
        public static string Encrypt(string value)
        {
            _aes.GenerateIV();
            var encryptor = _aes.CreateEncryptor();
            var valueBytes = Encoding.Unicode.GetBytes(value);
            return Convert.ToBase64String(_aes.IV)
                + ":"
                + Convert.ToBase64String(encryptor.TransformFinalBlock(valueBytes, 0, valueBytes.Length));
        }
    }
}
