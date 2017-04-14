using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using NUnit.Framework;
using RSB.Exceptions;
using RSB.Interfaces;
using RSB.Transports.Nats;
using RSB.Transports.RabbitMQ;

namespace RSB.Tests
{
    [TestFixture]
    public class BusTests
    {
        private IBus _busServer1;
        private IBus _busServer2;
        private IBus _busClient;

        private const string TestContent = "test";
        private const string Prefix1 = "-1";
        private const string Prefix2 = "-2";

        private const string Server1LogicalAddress = "Server1";
        private const string Server2LogicalAddress = "Server2";

        private const int WaitTimeoutMilliseconds = 500;

        [SetUp]
        public void Init()
        {
            _busServer1 = new Bus(new NatsTransport());
            _busServer2 = new Bus(new NatsTransport());
            _busClient = new Bus(new NatsTransport());
        }

        [TearDown]
        public void Deinit()
        {
            _busServer1.Shutdown();
            _busServer2.Shutdown();
            _busClient.Shutdown();
        }

        [Test]
        public async void CallNoLogicalAddress()
        {
            _busServer1.RegisterCallHandler<TestRpcRequest, TestRpcResponse>(req => new TestRpcResponse { Content = req.Content, SampleEnum = req.SampleEnum });

            var response = await _busServer1.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = TestContent, SampleEnum = SampleEnum.Third });

            Assert.AreEqual(TestContent, response.Content);
            Assert.AreEqual(SampleEnum.Third, response.SampleEnum);
        }

        [Test]
        public async void CallSameLogicalAddressTwoContracts()
        {
            _busServer1.RegisterCallHandler<TestRpcRequest, TestRpcResponse>(req => new TestRpcResponse { Content = req.Content }, "Test");
            _busServer1.RegisterCallHandler<TestRpcRequest2, TestRpcResponse2>(req => new TestRpcResponse2 { Content = req.Content }, "Test");

            var response1 = await _busClient.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = TestContent }, "Test");
            var response2 = await _busClient.Call<TestRpcRequest2, TestRpcResponse2>(new TestRpcRequest2 { Content = TestContent }, "Test");

            Assert.AreEqual(TestContent, response1.Content);
            Assert.AreEqual(TestContent, response2.Content);
        }

        [Test]
        public async void CallTwoDifferentLogicalAddresses()
        {
            _busServer1.RegisterCallHandler<TestRpcRequest, TestRpcResponse>(req => new TestRpcResponse { Content = req.Content + Prefix1 }, Server1LogicalAddress);
            _busServer2.RegisterCallHandler<TestRpcRequest, TestRpcResponse>(req => new TestRpcResponse { Content = req.Content + Prefix2 }, Server2LogicalAddress);

            var response1 = await _busServer1.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = TestContent }, Server1LogicalAddress);
            var response2 = await _busServer1.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = TestContent }, Server2LogicalAddress);

            Assert.AreEqual(TestContent + Prefix1, response1.Content);
            Assert.AreEqual(TestContent + Prefix2, response2.Content);
        }

        [Test]
        public async void CallException()
        {
            _busServer1.RegisterCallHandler<TestRpcRequest, TestRpcResponse>(req =>
            {
                throw new CustomException("Test exception")
                {
                    CustomProperty = "Test property"
                };
            });

            await Task.Delay(1000);

            try
            {
                await _busServer1.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest {Content = "error"});

                Assert.Fail("Exception not thrown");
            }
            catch (CustomException ex)
            {
                Assert.AreEqual("Test property", ex.CustomProperty);
                Assert.AreEqual("Test exception", ex.Message);
            }
            catch
            {
                Assert.Fail("Generic exception thrown instead of the custom one");
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public async void CallBuiltinException()
        {
            _busServer1.RegisterCallHandler<TestRpcRequest, TestRpcResponse>(req =>
            {
                throw new ArgumentException("Test exception");
            });

            await Task.Delay(1000);

            await _busServer1.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = "error" });
        }

        [Test]
        [ExpectedException(typeof(TimeoutException))]
        public async void CallTimeout()
        {
            _busServer1.RegisterAsyncCallHandler<TestRpcRequest, TestRpcResponse>(async req =>
            {
                await Task.Delay(2000);

                return new TestRpcResponse();
            });

            await _busClient.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = "error" }, timeoutSeconds: 1);
        }

        [Test]
        [ExpectedException(typeof(MessageReturnedException))]
        public async void CallMessageReturned()
        {
            await Task.Delay(1000);

            await _busClient.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = "error" });
        }

        [Test]
        public async void PublishNoLogicalAddress()
        {
            var receivedContent = "";

            _busServer1.RegisterQueueHandler<TestMessage>(msg => receivedContent = msg.Content);

            _busClient.Enqueue(new TestMessage { Content = TestContent });

            await Task.Delay(WaitTimeoutMilliseconds);

            Assert.AreEqual(TestContent, receivedContent);
        }

        [Test]
        public async void PublishTwoLogicalAddresses()
        {
            var receivedContent1 = "";
            var receivedContent2 = "";

            _busServer1.RegisterQueueHandler<TestMessage>(msg => receivedContent1 = msg.Content, Server1LogicalAddress);
            _busServer2.RegisterQueueHandler<TestMessage>(msg => receivedContent2 = msg.Content, Server2LogicalAddress);

            _busClient.Enqueue(new TestMessage { Content = TestContent + Prefix1 }, Server1LogicalAddress);

            await Task.Delay(WaitTimeoutMilliseconds);

            Assert.AreEqual(TestContent + Prefix1, receivedContent1);
            Assert.AreEqual("", receivedContent2);

            receivedContent1 = "";

            _busClient.Enqueue(new TestMessage { Content = TestContent + Prefix2 }, Server2LogicalAddress);

            await Task.Delay(WaitTimeoutMilliseconds);

            Assert.AreEqual("", receivedContent1);
            Assert.AreEqual(TestContent + Prefix2, receivedContent2);
        }

        [Test]
        public async void BroadcastNoLogicalAddress()
        {
            var receivedContent1 = "";
            var receivedContent2 = "";

            _busServer1.RegisterBroadcastHandler<TestMessage>(msg => receivedContent1 = msg.Content);
            _busServer2.RegisterBroadcastHandler<TestMessage>(msg => receivedContent2 = msg.Content);

            _busClient.Broadcast(new TestMessage { Content = TestContent });

            await Task.Delay(WaitTimeoutMilliseconds);

            Assert.AreEqual(TestContent, receivedContent1);
            Assert.AreEqual(TestContent, receivedContent2);
        }

        [Test]
        public async void BroadcastTwoLogicalAddresses()
        {
            var receivedContent1 = "";
            var receivedContent2 = "";

            _busServer1.RegisterBroadcastHandler<TestMessage>(msg => receivedContent1 = msg.Content, Server1LogicalAddress);
            _busServer2.RegisterBroadcastHandler<TestMessage>(msg => receivedContent2 = msg.Content, Server2LogicalAddress);

            _busClient.Broadcast(new TestMessage { Content = TestContent + Prefix1 }, Server1LogicalAddress);

            await Task.Delay(WaitTimeoutMilliseconds);

            Assert.AreEqual(TestContent + Prefix1, receivedContent1);
            Assert.AreEqual("", receivedContent2);

            receivedContent1 = "";

            _busClient.Broadcast(new TestMessage { Content = TestContent + Prefix2 }, Server2LogicalAddress);

            await Task.Delay(WaitTimeoutMilliseconds);

            Assert.AreEqual("", receivedContent1);
            Assert.AreEqual(TestContent + Prefix2, receivedContent2);

        }
    }

    public class CustomException : Exception
    {
        public CustomException()
        {

        }

        public CustomException(string message)
            : base(message)
        {

        }

        public string CustomProperty { get; set; }
    }

    public class TestMessage : Message
    {
        public string Content { get; set; }
    }

    public class TestMessageList : Message
    {
        public List<TestMessage> TestMessages { get; set; }
    }

    public enum SampleEnum
    {
        First,
        Second,
        Third
    }

    public class TestRpcRequest
    {
        public string Content { get; set; }
        public SampleEnum SampleEnum { get; set; }
    }

    public class TestRpcResponse
    {
        public string Content { get; set; }
        public SampleEnum SampleEnum { get; set; }
    }

    public class TestRpcRequest2
    {
        public string Content { get; set; }
    }

    public class TestRpcResponse2
    {
        public string Content { get; set; }
    }
}

