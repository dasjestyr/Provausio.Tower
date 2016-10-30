using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Provausio.Tower.Core;
using Xunit;

namespace Provausio.tower.Core.Tests
{
    public class HubTests
    {
        private const string ChallengeValue = "foo";
        private readonly Mock<ICryptoFunctions> _challengeGenerator;
        private readonly Mock<ISubscriptionStore> _subscriptionStore;
        private readonly Mock<IPublishQueue> _queue;
        private readonly Subscription _testSubscription;

        public HubTests()
        {
            _challengeGenerator = new Mock<ICryptoFunctions>();
            _challengeGenerator.Setup(m => m.GetHmacSha1Hash(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(ChallengeValue);
            _queue = new Mock<IPublishQueue>();
            _subscriptionStore = new Mock<ISubscriptionStore>();
            _testSubscription = new Subscription("http://test-topic.com", new Uri("http://test-callback.com"), "foo-bar");
        }

        [Fact]
        public void Ctor_SunnyDay_Initializes()
        {
            // arrange
            
            // act
            var hub = new Hub(_subscriptionStore.Object, _challengeGenerator.Object);

            // assert
            Assert.NotNull(hub);
        }

        [Fact]
        public void Ctor_NullStore_Throws()
        {
            // arrange
            
            // act

            // assert
            Assert.Throws<ArgumentNullException>(() => new Hub(null, _challengeGenerator.Object));
        }

        [Fact]
        public void Ctor_NullGenerator_Throws()
        {
            // arrange

            // act

            // assert
            Assert.Throws<ArgumentNullException>(() => new Hub(_subscriptionStore.Object, null));
        }

        [Fact]
        public async Task Subscribe_UrlNotFound_ReturnsFalse()
        {
            // arrange
            
            var handler = new FakeHandler(HttpStatusCode.NotFound, null);
            var hub = new Hub(_subscriptionStore.Object, _challengeGenerator.Object, _queue.Object, handler);

            // act
            var result = await hub.Subscribe(_testSubscription, "myToken");

            // assert
            Assert.False(result.SubscriptionSucceeded);
        }

        [Fact]
        public async Task Subscribe_ChallengeFailed_ReturnsFalse()
        {
            // arrange
            var subscriberChallengeReply = "bar"; // server sends "foo"
            var handler = new FakeHandler(HttpStatusCode.OK, subscriberChallengeReply);
            var hub = new Hub(_subscriptionStore.Object, _challengeGenerator.Object, _queue.Object, handler);

            // act
            var result = await hub.Subscribe(_testSubscription, "baz");

            // assert
            Assert.False(result.SubscriptionSucceeded);
        }

        [Fact]
        public async Task Subscribe_MissingChallengeResponse_ReturnsFalse()
        {
            // arrange
            var handler = new FakeHandler(HttpStatusCode.OK, null);
            var hub = new Hub(_subscriptionStore.Object, _challengeGenerator.Object, _queue.Object, handler);

            // act
            var result = await hub.Subscribe(_testSubscription, "baz");

            // assert
            Assert.False(result.SubscriptionSucceeded);
        }

        [Fact]
        public async Task Subscribe_Failure_DoesNotCallStore()
        {
            // arrange
            var store = new Mock<ISubscriptionStore>();
            store.Setup(m => m.Subscribe(It.IsAny<Subscription>()));
             
            var handler = new FakeHandler(HttpStatusCode.OK, null);
            var hub = new Hub(store.Object, _challengeGenerator.Object, _queue.Object, handler);

            // act
            var result = await hub.Subscribe(_testSubscription, "baz");

            // assert
            store.Verify(m => m.Subscribe(It.IsAny<Subscription>()), Times.Never);
        }

        [Fact]
        public async Task Subscribe_SunnyDay_ReturnsTrue()
        {
            // arrange
            var handler = new FakeHandler(HttpStatusCode.OK, ChallengeValue);
            var hub = new Hub(
                _subscriptionStore.Object,
                _challengeGenerator.Object, 
                _queue.Object, 
                handler);

            // act
            var result = await hub.Subscribe(_testSubscription, "verifyToken");

            // assert
            Assert.True(result.SubscriptionSucceeded);
        }

        [Fact]
        public async Task Subscribe_Success_CallsSubscriptionStore()
        {
            // arrange
            var subStore = new Mock<ISubscriptionStore>();
            subStore
                .Setup(m => m.Subscribe(It.IsAny<Subscription>()))
                .Returns(Task.FromResult(It.IsAny<SubscriptionResult>()));

            var handler = new FakeHandler(HttpStatusCode.OK, ChallengeValue);
            var hub = new Hub(subStore.Object, _challengeGenerator.Object, _queue.Object, handler);

            // act
            var result = await hub.Subscribe(_testSubscription, "verifyToken");

            // assert
            subStore.Verify(m => m.Subscribe(It.IsAny<Subscription>()), Times.Once);
        }

        [Fact]
        public void Publish_NullTopic_Throws()
        {
            // arrange
            var hub = new Hub(_subscriptionStore.Object, _challengeGenerator.Object, _queue.Object);

            // act

            // assert
            Assert.Throws<ArgumentNullException>(() => hub.Publish(null, new StringContent("test payload")));
        }

        [Fact]
        public void Publish_NullContent_Throws()
        {
            // arrange
            var hub = new Hub(_subscriptionStore.Object, _challengeGenerator.Object, _queue.Object);

            // act

            // assert
            Assert.Throws<ArgumentNullException>(() => hub.Publish("foo", null));
        }

        [Fact]
        public void Publish_AddsToQueue()
        {
            // arrange
            var queue = new Mock<IPublishQueue>();
            queue.Setup(m => m.Enqueue(It.IsAny<Publication>()));
            var hub = new Hub(_subscriptionStore.Object, _challengeGenerator.Object, queue.Object);

            // act
            hub.Publish("foo", new StringContent("test payload"));

            // assert
            queue.Verify(m => m.Enqueue(It.IsAny<Publication>()), Times.Once);
        }

        [Fact]
        public async Task PublishDirect_10Subscribers_NotifiesAll()
        {
            // arrange
            _subscriptionStore
                .Setup(m => m.GetSubscriptions(It.IsAny<string>()))
                .ReturnsAsync(GetSubs(10));

            var handler = new FakeHandler(HttpStatusCode.OK, "test");

            var hub = new Hub(
                _subscriptionStore.Object, 
                _challengeGenerator.Object, 
                _queue.Object, 
                handler);

            // act
            await hub.PublishDirect(new Publication("foo", new StringContent("test payload")));
            await Task.Delay(500);

            // assert
            Assert.Equal(10, handler.Count);
        }

        [Fact]
        public async Task PublishDirect_NoSubscribers_NoHttpCallsMade()
        {
            // arrange
            _subscriptionStore
                .Setup(m => m.GetSubscriptions(It.IsAny<string>()))
                .ReturnsAsync(GetSubs(0));

            var handler = new FakeHandler(HttpStatusCode.OK, "test");

            var hub = new Hub(
                _subscriptionStore.Object,
                _challengeGenerator.Object,
                _queue.Object,
                handler);

            // act
            await hub.PublishDirect(new Publication("foo", new StringContent("test payload")));
            await Task.Delay(500);

            // assert
            Assert.Equal(0, handler.Count);
        }

        [Fact]
        public async Task PublishDirect_FailedNotify_TriggersEvent()
        {
            // arrange
            _subscriptionStore
                .Setup(m => m.GetSubscriptions(It.IsAny<string>()))
                .ReturnsAsync(GetSubs(2));

            var handler = new FakeHandler(HttpStatusCode.NotFound, "test failure");

            var hub = new Hub(
                _subscriptionStore.Object,
                _challengeGenerator.Object,
                _queue.Object,
                handler);

            var failCount = 0;
            hub.PublishNotificationFailed += (o, s) => failCount++;

            // act
            await hub.PublishDirect(new Publication("foo", new StringContent("test payload")));
            await Task.Delay(500);

            // assert
            Assert.Equal(2, failCount);
        }

        [Fact]
        public async Task Publish_WhileDisposed_Throws()
        {
            // arrange
            _subscriptionStore
                .Setup(m => m.GetSubscriptions(It.IsAny<string>()))
                .ReturnsAsync(GetSubs(10));

            var handler = new FakeHandler(HttpStatusCode.OK, "test");

            var hub = new Hub(
                _subscriptionStore.Object,
                _challengeGenerator.Object,
                _queue.Object,
                handler);

            hub.Dispose();

            // act
            
            // assert
            await
                Assert.ThrowsAsync<ObjectDisposedException>(
                    () => hub.PublishDirect(new Publication("foo", new StringContent("test payload"))));
        }

        private static IEnumerable<Subscription> GetSubs(int count)
        {
            var subs = new List<Subscription>();
            
            for (var i = 0; i < count; i++)
            {
                subs.Add(new Subscription("foo", new Uri($"http://someplace.com/api/{i}")));
            }

            return subs;
        }
    }

    internal class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        private readonly string _content;

        public int Count { get; set; }

        public FakeHandler(HttpStatusCode code, string content)
        {
            _code = code;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_code);
            if(!string.IsNullOrEmpty(_content))
                response.Content = new StringContent(_content);

            Count++;
            return Task.FromResult(response);
        }
    }
}
