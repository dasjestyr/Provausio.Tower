using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Provausio.Tower.Core;

namespace Provausio.Tower.Api
{
    public class HmacSha1Hasher : IHasher
    {
        public string GetHmacSha1Hash(byte[] content, string salt)
        {
            if(string.IsNullOrEmpty(salt))
                throw new ArgumentNullException(nameof(salt));

            var saltBytes = Encoding.UTF8.GetBytes(salt);
            var alg = new HMACSHA1(saltBytes);

            var ms = new MemoryStream(content);
            var hashBytes = alg.ComputeHash(ms);

            var hex = new StringBuilder(hashBytes.Length * 2);

            foreach (var b in hashBytes)
                hex.Append($"{b:x2}");

            return hex.ToString();
        }
    }
}
