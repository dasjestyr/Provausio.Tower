using System;
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
        private readonly Mock<IChallengeGenerator> _challengeGenerator;
        private readonly Mock<ISubscriptionStore> _subscriptionStore;
        private readonly Uri _testUri;

        public HubTests()
        {
            _testUri = new Uri("http://my.testapi.com/api");
            _challengeGenerator = new Mock<IChallengeGenerator>();
            _challengeGenerator.Setup(m => m.GetChallenge(It.IsAny<object>())).Returns(ChallengeValue);

            _subscriptionStore = new Mock<ISubscriptionStore>();
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
            var hub = new Hub(_subscriptionStore.Object, _challengeGenerator.Object, handler);

            // act
            var result = await hub.Subscribe("my topic", _testUri, "myToken");

            // assert
            Assert.False(result.SubscriptionSucceeded);
        }

        [Fact]
        public async Task Subscribe_ChallengeFailed_ReturnsFalse()
        {
            // arrange
            var subscriberChallengeReply = "bar"; // server sends "foo"
            var handler = new FakeHandler(HttpStatusCode.OK, subscriberChallengeReply);
            var hub = new Hub(_subscriptionStore.Object, _challengeGenerator.Object, handler);

            // act
            var result = await hub.Subscribe("my topic", _testUri, "baz");

            // assert
            Assert.False(result.SubscriptionSucceeded);
        }

        [Fact]
        public async Task Subscribe_MissingChallengeResponse_ReturnsFalse()
        {
            // arrange
            var handler = new FakeHandler(HttpStatusCode.OK, null);
            var hub = new Hub(_subscriptionStore.Object, _challengeGenerator.Object, handler);

            // act
            var result = await hub.Subscribe("my topic", _testUri, "baz");

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
            var hub = new Hub(store.Object, _challengeGenerator.Object, handler);

            // act
            var result = await hub.Subscribe("my topic", _testUri, "baz");

            // assert
            store.Verify(m => m.Subscribe(It.IsAny<Subscription>()), Times.Never);
        }

        [Fact]
        public async Task Subscribe_SunnyDay_ReturnsTrue()
        {
            // arrange
            var handler = new FakeHandler(HttpStatusCode.OK, ChallengeValue);
            var hub = new Hub(_subscriptionStore.Object, _challengeGenerator.Object, handler);

            // act
            var result = await hub.Subscribe("my topic", _testUri, "verifyToken");

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
            var hub = new Hub(subStore.Object, _challengeGenerator.Object, handler);

            // act
            var result = await hub.Subscribe("my topic", _testUri, "verifyToken");

            // assert
            subStore.Verify(m => m.Subscribe(It.IsAny<Subscription>()), Times.Once);
        }
    }

    internal class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        private readonly string _content;

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

            return Task.FromResult(response);
        }
    }
}
