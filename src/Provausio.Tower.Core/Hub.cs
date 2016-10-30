using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Provausio.Tower.Core
{
    public class Hub : IPubSubHub, IDisposable
    {
        private const string VerifyTokenProperty = "hub.verify_token";
        private const string ModeProperty = "hub.mode";
        private const string ChallengeProperty = "hub.challenge";
        private const string TopicProperty = "hub.topic";

        private readonly Uri _hubLocation;
        private readonly ISubscriptionStore _subscriptionStore;
        private readonly ICryptoFunctions _cryptoFunctions;
        private readonly NotificationService _notificationService;
        private readonly HttpClient _httpClient;

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hub" /> class.
        /// </summary>
        /// <param name="hubLocation">The hub address.</param>
        /// <param name="subscriptionStore">The subscription store.</param>
        /// <param name="cryptoFunctions">The challenge generator.</param>
        /// <param name="queue">The queue.</param>
        /// <param name="messageHandler">The message handler.</param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public Hub(
            Uri hubLocation,
            ISubscriptionStore subscriptionStore, 
            ICryptoFunctions cryptoFunctions, 
            IPublishQueue queue,
            HttpMessageHandler messageHandler)
        {
            if(subscriptionStore == null)
                throw new ArgumentNullException(nameof(subscriptionStore));

            if(cryptoFunctions == null)
                throw new ArgumentNullException(nameof(cryptoFunctions));

            _hubLocation = hubLocation;
            _subscriptionStore = subscriptionStore;
            _cryptoFunctions = cryptoFunctions;
            _notificationService = new NotificationService(this, queue ?? new InMemoryPublishQueue());

            _httpClient = messageHandler == null
                ? new HttpClient()
                : new HttpClient(messageHandler);

            _notificationService.Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hub" /> class.
        /// </summary>
        /// <param name="hubLocation">The hub address.</param>
        /// <param name="subscriptionStore">The subscription store.</param>
        /// <param name="cryptoFunctions">The challenge generator.</param>
        /// <param name="queue">The queue.</param>
        public Hub(
            Uri hubLocation,
            ISubscriptionStore subscriptionStore, 
            ICryptoFunctions cryptoFunctions,
            IPublishQueue queue)
            : this(hubLocation, subscriptionStore, cryptoFunctions, queue, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hub" /> class.
        /// </summary>
        /// <param name="hubLocation">The hub address.</param>
        /// <param name="subscriptionStore">The subscription store.</param>
        /// <param name="cryptoFunctions">The challenge generator.</param>
        public Hub(
            Uri hubLocation,
            ISubscriptionStore subscriptionStore,
            ICryptoFunctions cryptoFunctions)
            : this(hubLocation, subscriptionStore, cryptoFunctions, null, null)
        {
        }

        /// <summary>
        /// Subscribes the callback to the specified topic.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="verifyToken">The verify token.</param>
        /// <returns></returns>
        public async Task<SubscriptionResult> Subscribe(
            Subscription subscription, 
            string verifyToken)
        {
            CheckDispose();
            
            var result = await VerifyCallback(subscription.Topic, subscription.Callback, verifyToken);

            if (!result.SubscriptionSucceeded)
                return result;

            if (!string.IsNullOrEmpty(subscription.Secret))
                subscription.Secret = _cryptoFunctions.Encrypt(subscription.Secret, subscription.Callback.ToString());

            await _subscriptionStore.Subscribe(subscription);

            return result;
        }

        /// <summary>
        /// Queues a task that notifies all subscribers of an event
        /// </summary>
        /// <param name="publication">The publication.</param>
        /// <exception cref="ArgumentNullException">Can't notify a subscriber without any info!</exception>
        public void Publish(Publication publication)
        {
            CheckDispose();

            if(publication == null)
                throw new ArgumentNullException(nameof(publication));

            _notificationService.Enqueue(publication);
        }

        /// <summary>
        /// Executes the notification task
        /// </summary>
        /// <param name="publication">The publication.</param>
        /// <returns></returns>
        public async Task PublishDirect(Publication publication)
        {
            CheckDispose();

            if (publication.HubLocation != _hubLocation)
                throw new RequestedHubMismatchException(string.Format(Strings.Error_HubLocationMismatch, _hubLocation, publication.HubLocation));

            var subscriptions = await _subscriptionStore.GetSubscriptions(publication.Topic);
            var subscriberList = subscriptions.ToList();
            if (!subscriberList.Any())
                return;
            
            var notifyTasks = subscriberList.Select(sub => Notify(publication, sub, publication.Payload));

            // TODO: notify if timeout
            await Task.WhenAll(notifyTasks);
        }

        private async Task Notify(Publication publication, Subscription subscription, HttpContent content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, subscription.Callback) {Content = content};
            request.Headers.Add("link", $"<{publication.HubLocation} />; rel=\"hub\", <{publication.Topic} />; rel=\"self\""); // required headers

            if (!string.IsNullOrEmpty(subscription.Secret))
            {
                var secret = _cryptoFunctions.Decrypt(subscription.Secret, subscription.Callback.ToString());

                /* Section 8 */
                var hash = _cryptoFunctions.GetHmacSha1Hash(
                    await content.ReadAsByteArrayAsync(),
                    secret);

                request.Headers.Add("X-Hub-Signature", hash);
            }

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var message = Strings.Error_SubscriberPublishFailed;
                if (response.Content != null)
                    message = $"{(int)response.StatusCode}:{await response.Content.ReadAsStringAsync()}";

                var args = new PublishNotificationFailureEventArgs(subscription, message);
                OnNotifyFailed(args);
            }
        }
        
        private async Task<SubscriptionResult> VerifyCallback(
            string topic, 
            Uri callback, 
            string verifyToken)
        {
            // section 5.3 - Verify Intent
            
            // using some request data to generate a unique hash as a challenge
            var uniqueString = $"{callback}|{DateTime.UtcNow.Millisecond}";
            var uniqueStringAsBytes = Encoding.UTF8.GetBytes(uniqueString);
            var challenge = _cryptoFunctions.GetHmacSha1Hash(uniqueStringAsBytes, uniqueString);

            // modify the callback uri to include required parameters (mode, topic, challenge) TODO: implement hub.lease
            var validationUrl = GetValidationUri(topic, callback, verifyToken, challenge, "subscribe");

            // call the callback 
            var request = new HttpRequestMessage(HttpMethod.Get, validationUrl);
            var response = await _httpClient.SendAsync(request);

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _notificationService.Stop();
            _httpClient.Dispose();
            _isDisposed = true;
        }

        private void CheckDispose()
        {
            if(_isDisposed)
                throw new ObjectDisposedException("Hub");
        }

        private void OnNotifyFailed(PublishNotificationFailureEventArgs args)
        {
            PublishNotificationFailed?.Invoke(this, args);
        }

        public event PublishNotificationFailureHandler PublishNotificationFailed;
    }

    public delegate void PublishNotificationFailureHandler(object sender, PublishNotificationFailureEventArgs e);

    public class InvalidHubRequestException : Exception
    {
        public InvalidHubRequestException(string message)
            : base(message)
        {
        }
    }

    public class RequestedHubMismatchException : Exception
    {
        public RequestedHubMismatchException(string message)
            : base(message)
        {
        }
    }
    
}
