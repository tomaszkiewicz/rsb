using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RSB.Interfaces;
using RSB.Serialization;
using RSB.Transports.RabbitMQ;

namespace RSB.Tests
{
    [TestFixture]
    class PollingQueueHandlerTests
    {
        private IBus _bus1;
        private IBus _bus2;
        private RabbitMqTransport _transport1;
        private RabbitMqTransport _transport2;

        [SetUp]
        public void Init()
        {
            _transport1 = RabbitMqTransport.FromConfigurationFile();
            _transport2 = RabbitMqTransport.FromConfigurationFile();

            _bus1 = new Bus(_transport1);
            _bus2 = new Bus(_transport2);

            Thread.Sleep(1000);
        }

        [TearDown]
        public void Deinit()
        {
            _bus1.Shutdown();
            _bus2.Shutdown();
        }

        [Test]
        public async Task TestGet()
        {
            var queueHandler = _transport1.GetRawPollingQueueHandler(new QueueInfo("TestMessage"));

            queueHandler.BindToExchange("TestMessage");

            _bus2.Enqueue(new TestMessage { Content = "Test" });

            await Task.Delay(1000);

            var item = queueHandler.GetItem();

            Assert.AreNotEqual(null, item);

            var serializer = new JsonSerializer();

            var msg = serializer.Deserialize<TestMessage>(item.Body);

            Assert.AreEqual("Test", msg.Content);
        }

        class TestMessage
        {
            public string Content { get; set; }
        }
    }
}
