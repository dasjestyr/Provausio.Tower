using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Provausio.Tower.Core
{
    public class Hub : IPubSubHub
    {
        private const string VerifyTokenProperty = "hub.verify_token";
        private const string ModeProperty = "hub.mode";
        private const string ChallengeProperty = "hub.challenge";
        private const string TopicProperty = "hub.topic";

        private readonly ISubscriptionStore _subscriptionStore;
        private readonly IChallengeGenerator _challengeGenerator;
        private readonly HttpMessageHandler _messageHandler;
        

        /// <summary>
        /// Initializes a new instance of the <see cref="Hub"/> class.
        /// </summary>
        /// <param name="subscriptionStore">The subscription store.</param>
        /// <param name="challengeGenerator">The challenge generator.</param>
        /// <param name="messageHandler">The message handler.</param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public Hub(
            ISubscriptionStore subscriptionStore, 
            IChallengeGenerator challengeGenerator, 
            HttpMessageHandler messageHandler = null)
        {
            if(subscriptionStore == null)
                throw new ArgumentNullException(nameof(subscriptionStore));

            if(challengeGenerator == null)
                throw new ArgumentNullException(nameof(challengeGenerator));

            _subscriptionStore = subscriptionStore;
            _challengeGenerator = challengeGenerator;
            _messageHandler = messageHandler;
        }

        public async Task<SubscriptionResult> Subscribe(
            string topic, 
            Uri callback, 
            string verifyToken)
        {
            var result = await VerifyCallback(topic, callback, verifyToken);

            if (!result.SubscriptionSucceeded)
                return result;

            var subscription = new Subscription(topic, callback);
            await _subscriptionStore.Subscribe(subscription);

            return result;
        }

        private async Task<SubscriptionResult> VerifyCallback(
            string topic, 
            Uri callback, 
            string verifyToken)
        {
            // section 5.3 - Verify Intent
            
            // this is just a random format to try and generate a unique challenge. It doesn't need to be recreated later.
            var challenge = _challengeGenerator.GetChallenge($"{callback}|{DateTime.UtcNow.Millisecond}");

            // modify the callback uri to include required parameters (mode, topic, challenge) TODO: implement hub.lease
            var validationUrl = GetValidationUri(topic, callback, verifyToken, challenge, "subscribe");

            // call the callback 
            var client = _messageHandler == null
                ? new HttpClient()
                : new HttpClient(_messageHandler);

            var request = new HttpRequestMessage(HttpMethod.Get, validationUrl);
            var response = await client.SendAsync(request);

            return await VerifyDetails(response, challenge);
        }

        private static async Task<SubscriptionResult> VerifyDetails(HttpResponseMessage response, string challenge)
        {
            // section 5.3.1

            var result = new SubscriptionResult(false, string.Empty);

            // verify response code
            if (response.IsSuccessStatusCode)
            {
                result.SubscriptionSucceeded = true;
            }
            else
            {
                result.SubscriptionSucceeded = false;
                result.Reason = $"Invalid response code ({response.StatusCode})";
                return result;
            }

            // verify challenge

            if (response.Content == null)
            {
                result.SubscriptionSucceeded = false;
                result.Reason = "Could not find challenge in subscriber response.";
                return result;
            }

            var challengeResponse = await response.Content.ReadAsStringAsync();
            challengeResponse = challengeResponse.Trim('"');

            if (challengeResponse.Equals(challenge))
            {
                result.SubscriptionSucceeded = true;
            }
            else
            {
                result.SubscriptionSucceeded = false;
                result.Reason = "Challenge failed";
            }

            return result;
        }

        private static Uri GetValidationUri(
            string topic, 
            Uri callback, 
            string verifyToken, 
            string challenge, 
            string mode)
        {
            var uriBuilder = new UriBuilder(callback);
            var newQuery = $"{TopicProperty}={topic}&{VerifyTokenProperty}={verifyToken}&{ModeProperty}={mode}&{ChallengeProperty}={challenge}";

            uriBuilder.Query = uriBuilder.Query != null && uriBuilder.Query.Length > 1
                ? $"{uriBuilder.Query.Substring(1)}&{newQuery}"
                : newQuery;

            return uriBuilder.Uri;
        }
    }
}
