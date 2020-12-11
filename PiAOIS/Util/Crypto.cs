using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PiAOIS.Util
{
    public static class Crypto
    {
        private const int _iterations = 6666;
        private static readonly AesCng _aes = new AesCng() { Mode = CipherMode.CBC };
        private static readonly byte[] _salt = Convert.FromBase64String("vOVH6sdmpNWjRRIqCc7rdxs01lwHzfr3");
        public static void SetPassword(string password)
        {
            _aes.Key = new Rfc2898DeriveBytes(password, _salt, _iterations, HashAlgorithmName.SHA256)
                .GetBytes(_aes.KeySize / 8);
        }
        public static double Decrypt(string cipher)
        {
            var vs = cipher.Split(':');
            //Incoming value contains both IV and cipher
            if (vs.Length != 2)
                return double.NaN;
            try
            {
                _aes.IV = Convert.FromBase64String(vs[0]);
                var value = Convert.FromBase64String(vs[1]);
                var decryptor = _aes.CreateDecryptor();
                string vx = Encoding.Unicode.GetString
                    (decryptor.TransformFinalBlock(value, 0, value.Length));
                if (!double.TryParse(vx, out double result))
                    return double.NaN;
                return result;
            }
            catch (Exception)
            {
                return double.NaN;
            }
        }
    }
}
