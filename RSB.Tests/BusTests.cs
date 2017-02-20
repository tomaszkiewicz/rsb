using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RSB.Exceptions;
using RSB.Interfaces;
using RSB.Transports.RabbitMQ;
using Xunit;

namespace RSB.Tests
{
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

        public BusTests()
        {
            var transport1 = RabbitMqTransport.FromConfigurationFile();
            var transport2 = RabbitMqTransport.FromConfigurationFile();
            var transport3 = RabbitMqTransport.FromConfigurationFile();

            Task.WaitAll(transport1.WaitForConnection(), transport2.WaitForConnection(), transport3.WaitForConnection());

            _busServer1 = new Bus(transport1);
            _busServer2 = new Bus(transport2);
            _busClient = new Bus(transport3);
        }
        
        public void Deinit()
        {
            _busServer1.Shutdown();
            _busServer2.Shutdown();
            _busClient.Shutdown();

            _busServer1 = null;
            _busServer2 = null;
            _busClient = null;
        }

        [Fact]
        public async Task CallNoLogicalAddress()
        {
            _busServer1.RegisterCallHandler<TestRpcRequest, TestRpcResponse>(req => new TestRpcResponse { Content = req.Content, SampleEnum = req.SampleEnum });

            var response = await _busServer1.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = TestContent, SampleEnum = SampleEnum.Third });

            Assert.Equal(TestContent, response.Content);
            Assert.Equal(SampleEnum.Third, response.SampleEnum);
        }

        [Fact]
        public async Task CallSameLogicalAddressTwoContracts()
        {
            _busServer1.RegisterCallHandler<TestRpcRequest, TestRpcResponse>(req => new TestRpcResponse { Content = req.Content }, "Test");
            _busServer1.RegisterCallHandler<TestRpcRequest2, TestRpcResponse2>(req => new TestRpcResponse2 { Content = req.Content }, "Test");

            var response1 = await _busClient.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = TestContent + Prefix1 }, "Test");
            var response2 = await _busClient.Call<TestRpcRequest2, TestRpcResponse2>(new TestRpcRequest2 { Content = TestContent + Prefix2 }, "Test");

            Assert.Equal(TestContent + Prefix1, response1.Content);
            Assert.Equal(TestContent + Prefix2, response2.Content);
        }

        [Fact]
        public async Task CallTwoDifferentLogicalAddresses()
        {
            _busServer1.RegisterCallHandler<TestRpcRequest, TestRpcResponse>(req => new TestRpcResponse { Content = req.Content + Prefix1 }, Server1LogicalAddress);
            _busServer2.RegisterCallHandler<TestRpcRequest, TestRpcResponse>(req => new TestRpcResponse { Content = req.Content + Prefix2 }, Server2LogicalAddress);

            await Task.Delay(1000);

            var response1 = await _busClient.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = TestContent }, Server1LogicalAddress);
            var response2 = await _busClient.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = TestContent }, Server2LogicalAddress);

            Assert.Equal(TestContent + Prefix1, response1.Content);
            Assert.Equal(TestContent + Prefix2, response2.Content);
        }

        [Fact]
        public async Task CallException()
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

                Assert.True(false, "Exception not thrown");
            }
            catch (CustomException ex)
            {
                Assert.Equal("Test property", ex.CustomProperty);
                Assert.Equal("Test exception", ex.Message);
            }
            catch
            {
                Assert.True(false, "Generic exception thrown instead of the custom one");
            }
        }

        [Fact]
        public async Task CallBuiltinException()
        {
            _busServer1.RegisterCallHandler<TestRpcRequest, TestRpcResponse>(req =>
            {
                throw new ArgumentException("Test exception");
            });

            await Task.Delay(1000);

            await AssertException<ArgumentException>(async () => await _busServer1.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = "error" }));
        }

        [Fact]
        public async Task CallTimeout()
        {
            _busServer1.RegisterAsyncCallHandler<TestRpcRequest, TestRpcResponse>(async req =>
            {
                await Task.Delay(2000);

                return new TestRpcResponse();
            });

            await Task.Delay(1000);

            await AssertException<TimeoutException>(async () => await _busClient.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = "error" }, timeoutSeconds: 1));
            
            await Task.Delay(2000);
        }

        [Fact]
        public async Task CallMessageReturned()
        {
            await Task.Delay(1000);

            await AssertException<MessageReturnedException>(async () => await _busClient.Call<TestRpcRequest, TestRpcResponse>(new TestRpcRequest { Content = "error" }, "NonExisting"));
        }

        [Fact]
        public async Task PublishNoLogicalAddress()
        {
            var receivedContent = "";

            _busServer1.RegisterQueueHandler<TestMessage>(msg => receivedContent = msg.Content);

            _busClient.Enqueue(new TestMessage { Content = TestContent });

            await Task.Delay(WaitTimeoutMilliseconds);

            Assert.Equal(TestContent, receivedContent);
        }

        [Fact]
        public async Task PublishTwoLogicalAddresses()
        {
            var receivedContent1 = "";
            var receivedContent2 = "";

            _busServer1.RegisterQueueHandler<TestMessage>(msg => receivedContent1 = msg.Content, Server1LogicalAddress);
            _busServer2.RegisterQueueHandler<TestMessage>(msg => receivedContent2 = msg.Content, Server2LogicalAddress);

            _busClient.Enqueue(new TestMessage { Content = TestContent + Prefix1 }, Server1LogicalAddress);

            await Task.Delay(WaitTimeoutMilliseconds);

            Assert.Equal(TestContent + Prefix1, receivedContent1);
            Assert.Equal("", receivedContent2);

            receivedContent1 = "";

            _busClient.Enqueue(new TestMessage { Content = TestContent + Prefix2 }, Server2LogicalAddress);

            await Task.Delay(WaitTimeoutMilliseconds);

            Assert.Equal("", receivedContent1);
            Assert.Equal(TestContent + Prefix2, receivedContent2);
        }

        [Fact]
        public async Task BroadcastNoLogicalAddress()
        {
            var receivedContent1 = "";
            var receivedContent2 = "";

            _busServer1.RegisterBroadcastHandler<TestMessage>(msg => receivedContent1 = msg.Content);
            _busServer2.RegisterBroadcastHandler<TestMessage>(msg => receivedContent2 = msg.Content);

            _busClient.Broadcast(new TestMessage { Content = TestContent });

            await Task.Delay(WaitTimeoutMilliseconds);

            Assert.Equal(TestContent, receivedContent1);
            Assert.Equal(TestContent, receivedContent2);
        }

        [Fact]
        public async Task BroadcastTwoLogicalAddresses()
        {
            var receivedContent1 = "";
            var receivedContent2 = "";

            _busServer1.RegisterBroadcastHandler<TestMessage>(msg => receivedContent1 = msg.Content, Server1LogicalAddress);
            _busServer2.RegisterBroadcastHandler<TestMessage>(msg => receivedContent2 = msg.Content, Server2LogicalAddress);

            _busClient.Broadcast(new TestMessage { Content = TestContent + Prefix1 }, Server1LogicalAddress);

            await Task.Delay(WaitTimeoutMilliseconds);

            Assert.Equal(TestContent + Prefix1, receivedContent1);
            Assert.Equal("", receivedContent2);

            receivedContent1 = "";

            _busClient.Broadcast(new TestMessage { Content = TestContent + Prefix2 }, Server2LogicalAddress);

            await Task.Delay(WaitTimeoutMilliseconds);

            Assert.Equal("", receivedContent1);
            Assert.Equal(TestContent + Prefix2, receivedContent2);

        }

        private async Task AssertException<T>(Func<Task> func) where T : Exception
        {
            try
            {
                await func();

                Assert.True(false, "No exception caught.");
            }
            catch (T)
            {
                // ok :)
            }
            catch (Exception ex)
            {
                Assert.True(false, $"Invalid exception thrown. Expecting {typeof(T).FullName} but was {ex.GetType().FullName}");
            }
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

    public abstract class Message
    {
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