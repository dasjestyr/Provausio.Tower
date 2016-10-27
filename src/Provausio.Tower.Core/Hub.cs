﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Provausio.Tower.Core
{
    public class Hub : IPubSubHub, IDisposable
    {
        private const string VerifyTokenProperty = "hub.verify_token";
        private const string ModeProperty = "hub.mode";
        private const string ChallengeProperty = "hub.challenge";
        private const string TopicProperty = "hub.topic";

        private readonly ISubscriptionStore _subscriptionStore;
        private readonly IChallengeGenerator _challengeGenerator;
        private readonly HttpClient _httpClient;

        private bool _isDisposed;
        private readonly NotificationService _notificationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hub"/> class.
        /// </summary>
        /// <param name="subscriptionStore">The subscription store.</param>
        /// <param name="challengeGenerator">The challenge generator.</param>
        /// <param name="queue"></param>
        /// <param name="messageHandler">The message handler.</param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public Hub(
            ISubscriptionStore subscriptionStore, 
            IChallengeGenerator challengeGenerator, 
            IPublishQueue queue,
            HttpMessageHandler messageHandler)
        {
            if(subscriptionStore == null)
                throw new ArgumentNullException(nameof(subscriptionStore));

            if(challengeGenerator == null)
                throw new ArgumentNullException(nameof(challengeGenerator));
            
            _subscriptionStore = subscriptionStore;
            _challengeGenerator = challengeGenerator;
            _notificationService = new NotificationService(this, queue ?? new InMemoryPublishQueue());

            _httpClient = messageHandler == null
                ? new HttpClient()
                : new HttpClient(messageHandler);

            _notificationService.Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hub" /> class.
        /// </summary>
        /// <param name="subscriptionStore">The subscription store.</param>
        /// <param name="challengeGenerator">The challenge generator.</param>
        /// <param name="queue">The queue.</param>
        public Hub(
            ISubscriptionStore subscriptionStore, 
            IChallengeGenerator challengeGenerator,
            IPublishQueue queue)
            : this(subscriptionStore, challengeGenerator, queue, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hub"/> class.
        /// </summary>
        /// <param name="subscriptionStore">The subscription store.</param>
        /// <param name="challengeGenerator">The challenge generator.</param>
        public Hub(
            ISubscriptionStore subscriptionStore,
            IChallengeGenerator challengeGenerator)
            : this(subscriptionStore, challengeGenerator, null, null)
        {
        }

        /// <summary>
        /// Subscribes the callback to the specified topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="verifyToken">The verify token.</param>
        /// <returns></returns>
        public async Task<SubscriptionResult> Subscribe(
            string topic, 
            Uri callback, 
            string verifyToken)
        {
            CheckDispose();
            var result = await VerifyCallback(topic, callback, verifyToken);

            if (!result.SubscriptionSucceeded)
                return result;

            var subscription = new Subscription(topic, callback);
            await _subscriptionStore.Subscribe(subscription);

            return result;
        }

        /// <summary>
        /// Queues a task that notifies all subscribers of an event
        /// </summary>
        /// <param name="topic">The topic identifier.</param>
        /// <param name="payload">The payload that will be sent to subscribers.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Can't notify a subscriber without any info!</exception>
        public void Publish(string topic, HttpContent payload)
        {
            CheckDispose();

            if(payload == null)
                throw new ArgumentNullException(nameof(payload), "Can't notify a subscriber without any info!");

            _notificationService.Enqueue(new Publication(topic, payload));
        }

        /// <summary>
        /// Executes the notification task
        /// </summary>
        /// <param name="publication">The publication.</param>
        /// <returns></returns>
        internal async Task Publish(Publication publication)
        {
            var subscriptions = await _subscriptionStore.GetSubscriptions(publication.Topic);
            var subscriberList = subscriptions.ToList();
            if (!subscriberList.Any())
                return;

            var notifyTasks = subscriberList.Select(sub => Notify(sub, publication.Payload));

            // TODO: notify if timeout
            await Task.WhenAll(notifyTasks);
        }

        private async Task Notify(Subscription subscription, HttpContent content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, subscription.Callback) {Content = content};

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var message = "The subscriber's endpoint did not return a success code.";
                    if (response.Content != null)
                        message = $"{(int)response.StatusCode}:{await response.Content.ReadAsStringAsync()}";

                    var args = new PublishNotificationFailureEventArgs(subscription, message);
                    OnNotifyFailed(args);
                }
            }
            catch (AggregateException ex)
            {
                var exMessage = ex.InnerExceptions[0];
                var message = $"Notification failed do to an exception. {exMessage.Message}";
                var args = new PublishNotificationFailureEventArgs(subscription, message);
                OnNotifyFailed(args);
            }
            catch(Exception ex)
            {
                var message = $"Notification failed do to an exception. {ex.Message}";
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
            
            // this is just a random format to try and generate a unique challenge. It doesn't need to be recreated later.
            var challenge = _challengeGenerator.GetChallenge($"{callback}|{DateTime.UtcNow.Millisecond}");

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
}