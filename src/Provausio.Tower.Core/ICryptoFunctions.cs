namespace Provausio.Tower.Core
{
    public interface ICryptoFunctions
    {
        /// <summary>
        /// When implemented, returns a 40 bytes hexadecimal representation of a SHA1 signature (RFC3174) using the HMAC algorithm (RFC2104)
        /// </summary>
        /// <param name="content">The plainText that will be signed.</param>
        /// <param name="salt">The secret/salt.</param>
        /// <returns></returns>
        string GetHmacSha1Hash(byte[] content, string salt);

        /// <summary>
        /// Encrypts the specified plain text.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="passphrase">The passphrase.</param>
        /// <returns></returns>
        string Encrypt(string plainText, string passphrase);

        /// <summary>
        /// Decrypts the specified cyphertext
        /// </summary>
        /// <param name="cypherText">The plainText.</param>
        /// <param name="passphrase">The passphrase.</param>
        /// <returns></returns>
        string Decrypt(string cypherText, string passphrase);
    }
}