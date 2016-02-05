RSB
=========

## Installation

At this moment, the only fully implemented transport is RabbitMQ, so to install RSB with RabbitMQ transport just install the following NuGet package:

```
Install-Package RSB.Transports.RabbitMQ
```

## Basic usage

### Creating service bus

The simplest way to create new bus object is to use `Bus` class constructor with transport class instance:

```
var bus = new Bus(new RabbitMqTransport("your.rabbitmq.host.com"));
```

Above example uses default RabbitMQ settings. If you need to specify your own settings use another constructor:

```
RabbitMqTransport(string hostName, string user = "guest", string password = "guest", string virtualHost = "/", ushort heartbeat = 5, bool useDurableExchanges = true)
```

...or provide `IConnectionFactory` object (useful especially with IoC):

```
RabbitMqTransport(IConnectionFactory factory, bool useDurableExchanges = true)
```

RabbitMQ transport automatically connects to RabbitMQ server upon initialization.

### Creating contract messages

RSB is message-based solution, so to exchange messages between two nodes you need to define them and share these definitions between all project that use RSB.
RSB can use any JSON-serializable (at this time, only JSON serialization is implemented) class as a contract message.
The only requirement is that the class has to have default constructor.

#### Naming conventions

We recommend using the following conventions:

* For RPC contracts use `-Request` and `-Response` suffixes, eg. `SampleRequest` and `SampleResponse`
* For enqueued and broadcasted messages use `-Message` suffix, eg. `SampleMessage`.

### Queuing messages

The simplest pattern implemented in RSB is queue of messages. 
In this pattern sender enqueues message to be processed by workers.

Let's define contract:

```
public class SampleMessage 
{
    public string Content { get; set; }
}
```

Then, on receiver side (worker), register queue handler:

```
bus.RegisterQueueHandler<SampleMessage>(msg => Console.WriteLine(msg.Content));
```

You can also use async/await version:

```
bus.RegisterAsyncQueueHandler<SampleMessage>(async msg => await ...);
```

Next, on the sender side, enqueue new message:

```
bus.Enqueue(new SampleMessage() { Content = "Test" });
```

The message will be send to RabbitMQ and then dispatched to one (and only one) of connected workers (who have registered handlers) - so this pattern provides you load balancing out ot the box.

If you'd like to send message, use broadcast pattern.

### Broadcasting messages

Broadcasting allows sender to send message to multiple nodes.

We use the same contract as before:

```
public class SampleMessage 
{
    public string Content { get; set; }
}
```

On receiver side (multiple nodes) we define broadcast handler:

```
bus.RegisterBroadcastHandler<SampleMessage>(msg => Console.WriteLine(msg.Content));
```

Or it's async/await version:

```
bus.RegisterAsyncBroadcastHandler<SampleMessage>(async msg => await ...);
```

Then, on sender side, we broadcast new message:

```
bus.Broadcast(new SampleMessage() { Content = "Test" });
```

The message will be send to RabbitMQ and then dispatched to all connected workers (who have registered handlers). Every worker will receive copy of the message.

### Remote procedure calls

Another pattern implemented in RSB is Remote Procedure Call (RPC). 
In this pattern caller sends request message to callee, callee processes message and sends response to caller.

RPC has following behaviours:

* If there is no callee available then you will receive `MessageReturnedException`.
* If there are multiple callee available all requests will be load balanced.
* It is possible to specify timeout period (which defaults to 60 seconds) - after that period caller will receive `TimeoutException`.
* If callee is busy and cannot receive request from RabbitMQ in timeout period, the request will be discarded (and caller will receive `TimeoutException`) and will never reach any callee.
* If callee cannot complete received request within timeout period, the caller will receive `TimeoutException` and response message from callee will be discarded (caller will see warning about uncorrelated message id in it's logs) - so be careful and set high timeout for time consuming operations.
* RSB automatically catches all exceptions thrown on callee during execution and passes it to caller to be rethrown. More on this feature in advanced section below.

Let's define request and response contracts:

```
public class CalculatorAddRequest
{
    public int NumberA { get; set; }
    public int NumberB { get; set; }
}

public class CalculatorAddResponse 
{
    public int Result { get; set; }
}
```

First, on callee side, register RPC handler:

```
bus.RegisterCallHandler<CalculatorAddRequest, CalculatorAddResponse>(req => new CalculatorAddResponse { Result = req.NumberA + req.NumberB });
```

Of course there is async/await version (but in this example makes no sense):

```
bus.RegisterAsyncCallHandler<CalculatorAddRequest, CalculatorAddResponse>(async req => await ...);
```

Then, on caller side, we use `Call` method to initiate RPC:

```
var result = await bus.Call<CaluclatorAddRequest, CalculatorAddResponse>(new CalculatorAddRequest { NumberA = 5, NumberB = 10 });
```

## Advanced usage

### Using logical addresses

For all sender/caller methods (`Enqueue`, `Broadcast`, `Call`) you can specify optional logical address. 
Logical address allows you to create separate routing partitions and by doing that - distinct receivers groups.

Let's say we want to implement weather service. Every weather station, located in different cities, will handle RPC calls.

In Warsaw weather station, we register handler using custom logical address:

```
bus.RegisterAsyncCallHandler<WeatherRequest, WeatherResponse>(async req => await GetWeatherInfo(), "Warsaw");
```

In other station, located in London we do similar thing:

```
bus.RegisterAsyncCallHandler<WeatherRequest, WeatherResponse>(async req => await GetWeatherInfo(), "London");
```

Now we'd like to receive weather data from both stations:

```
var warsawData = await bus.Call<WeatherRequest, WeatherResponse>(new WeatherRequest(), "Warsaw");
var londonData = await bus.Call<WeatherRequest, WeatherResponse>(new WeatherRequest(), "London");
```

The same pattern works for `Enqueue` and `Broadcast` - only handlers with the same logical address will receive enqueued/broadcasted messages.

### Timeouts

For RPC, you can specify timeout you'd like to wait for response. For example, if getting weather data takes longer than default 60 seconds, we can extend this period by specifying own timeout:

```
var timeout = 180;

var warsawData = await bus.Call<WeatherRequest, WeatherResponse>(new WeatherRequest(), "Warsaw", timeout);
var londonData = await bus.Call<WeatherRequest, WeatherResponse>(new WeatherRequest(), "London", timeout);
```

### Preparing serializers and transports

RSB in current version uses JSON.Net serializer. This serializer caches some data at first use, so sending first message can take longer than subsequent messages.
If latency of sending first message is important, you can use `PrepareEnqueue`, `PrepareBroadcast` and `PrepareCall` methods to prepare the serializer caches.

These methods also allows you to check if serialization and deserialization of messages occurs without any problems.

Calling these methods may invoke some preparations on transport layer as well. For example in RabbitMQ transport preparing declares required exchanges and speeds up sending first message.

### Exceptions passing

RSB can pass exceptions catched during RPC execution in callee to caller and rethrown them on it.
Exceptions are matched to its types by class full name and to allow passing exception caller and callee have to have reference to exception class.

## Known issues

No known issues at the moment.


## Future plans

* Throtthling at receiver level to provide way to limit concurrency of handling request other than implementing own TaskFactory, integrated with underlying transports.
* Provide way to set transport-specific settings for handler registration.
* Remove dependency on NLog.
* Provide methods to create RSB using boostrapper-style initialization.
* Add settings to use message types with namespaces on RabbitMQ transport.

## Release history

* 2015-12-11 - v0.9.* - Initial public release