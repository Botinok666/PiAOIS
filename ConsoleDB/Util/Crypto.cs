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
        //Инициализация - установка пароля, указанного в hardcode стиле
        //RFC2898 - можно сказать, процедура получения ключа для алгоритма шифрования из пароля
        //Ключ должен быть определённой длины, в данном случае используется значение по умолчанию -
        //это 256 бит. RFC2898 берёт пароль и соль как начальное значение алгоритма, и потом
        //указанное число итераций применяет хэш функцию, тут выбрана SHA256
        //Результат - ключ длиной 256 бит, как и требуется для AES256
        public static void Init()
        {
            _aes.Key = new Rfc2898DeriveBytes(_pass, _salt, _iterations, HashAlgorithmName.SHA256)
                .GetBytes(_aes.KeySize / 8);
        }
        //Для шифрования сначала генерируется IV, затем входная строка преобразовывается в массив типа byte[]
        //Результат - IV:value, обе половинки результата представлены в формате base64
        //Поскольку шифруется всего один блок данных, сразу вызывается метод TransformFinalBlock
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
