namespace Provausio.Tower.Core
{
    public interface IChallengeGenerator
    {
        /// <summary>
        /// Gets the challenge.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns></returns>
        string GetChallenge(object parameter);
    }
}