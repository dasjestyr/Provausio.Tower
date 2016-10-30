namespace Provausio.Tower.Core
{
    public interface IHasher
    {
        /// <summary>
        /// Gets the challenge.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="salt">The salt.</param>
        /// <returns></returns>
        string GetHmacSha1Hash(byte[] content, string salt);
    }
}