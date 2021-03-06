﻿using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Provausio.Tower.Core;
using Xunit;

namespace Provausio.tower.Core.Tests
{
    public class NotificationServiceTests
    {
        private readonly Mock<IPublishQueue> _queueMock;
        private readonly Mock<ISubscriptionStore> _subStoreMock;
        private readonly Mock<ICryptoFunctions> _generator;
        private readonly Mock<IPubSubHub> _hub;
        private readonly ConcurrentQueue<Publication> _testQueue;
        private readonly Uri _hubLocation;
        private Publication _testPublication;

        public NotificationServiceTests()
        {
            _subStoreMock = new Mock<ISubscriptionStore>();
            _generator = new Mock<ICryptoFunctions>();
            _hub = new Mock<IPubSubHub>();

            _testQueue = new ConcurrentQueue<Publication>();
            _queueMock = new Mock<IPublishQueue>();
            _queueMock.Setup(m => m.Enqueue(It.IsAny<Publication>()));
            _hubLocation = new Uri("http://localhost:61616");
            _testPublication = new Publication("http://test-topic.com", new StringContent("test string"), _hubLocation);
        }

        [Fact]
        public void Ctor_NullHub_Throws()
        {
            // arrange
            
            // act

            // assert
            Assert.Throws<ArgumentNullException>(() => new NotificationService(null, _queueMock.Object));
        }

        [Fact]
        public void Ctor_NullQueue_Throws()
        {
            // arrange
            var hub = new Hub(_hubLocation, _subStoreMock.Object, _generator.Object, _queueMock.Object);

            // act

            // assert
            Assert.Throws<ArgumentNullException>(() => new NotificationService(hub, null));
        }

        [Fact]
        public async Task Start_QueueGetsProcessed()
        {
            // arrange
            _queueMock.Setup(m => m.Dequeue()).Returns(() =>
            {
                Publication p;
                _testQueue.TryDequeue(out p);
                return p;
            });

            _hub.Setup(m => m.PublishDirect(It.IsAny<Publication>()))
                .Returns(Task.CompletedTask);
            
            var service = new NotificationService(_hub.Object, _queueMock.Object);

            _testQueue.Enqueue(_testPublication);

            // act
            service.Start();
            await Task.Delay(50);

            // assert
            Assert.Equal(0, _testQueue.Count);
            _hub.Verify(m => m.PublishDirect(It.IsAny<Publication>()), Times.Once);
        }

        [Fact]
        public async Task Stop_QueueStopsProcessing()
        {
            // arrange
            _queueMock.Setup(m => m.Dequeue()).Returns(() =>
            {
                Publication p;
                _testQueue.TryDequeue(out p);
                return p;
            });

            _hub.Setup(m => m.PublishDirect(It.IsAny<Publication>()))
                .Returns(Task.CompletedTask);

            var service = new NotificationService(_hub.Object, _queueMock.Object, emptyQueueDelay: 1);
            service.Start();

            // act
            await Task.Delay(100);
            service.Stop();

            _testQueue.Enqueue(_testPublication);

            await Task.Delay(100);

            // assert
            Assert.Equal(1, _testQueue.Count);
            _hub.Verify(m => m.PublishDirect(It.IsAny<Publication>()), Times.Never);
        }

        [Fact]
        public void Enqueue_CallsInternalQueue()
        {
            // arrange
            _queueMock.Setup(m => m.Enqueue(It.IsAny<Publication>()));
            var service = new NotificationService(_hub.Object, _queueMock.Object);

            // act
            service.Enqueue(_testPublication);

            // assert
            _queueMock.Verify(m => m.Enqueue(It.IsAny<Publication>()), Times.Once);
        }

        [Fact]
        public void Dequeue_CallsInternalQueue()
        {
            // arrange
            _queueMock.Setup(m => m.Dequeue());
            var service = new NotificationService(_hub.Object, _queueMock.Object);

            // act
            service.Enqueue(_testPublication);
            var x = service.Dequeue();

            // assert
            _queueMock.Verify(m => m.Dequeue(), Times.Once);
        }
    }
}
