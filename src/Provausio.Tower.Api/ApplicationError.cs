namespace Provausio.Tower.Api
{
    /// <summary>
    /// Serializable error class
    /// </summary>
    public class ApplicationError
    {
        public string Message { get; set; }

        public ApplicationError(string clientMessage)
        {
            Message = clientMessage;
        }
    }
}
