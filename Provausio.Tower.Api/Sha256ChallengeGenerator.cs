using System;
using System.Security.Cryptography;
using System.Text;
using Provausio.Tower.Core;

namespace Provausio.Tower.Api
{
    public class Sha256ChallengeGenerator : IChallengeGenerator
    {
        public string GetChallenge(object parameter)
        {
            if(!(parameter is string))
                throw new ArgumentException("Expected string", nameof(parameter));

            var asBytes = Encoding.UTF8.GetBytes(parameter.ToString());

            var alg = MD5.Create();
            var hash = alg.ComputeHash(asBytes);
            
            var sb = new StringBuilder();
            foreach (var b in hash)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }
    }
}
