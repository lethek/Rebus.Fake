using Hypothesist;
using Hypothesist.Rebus;

using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Transport;
using Rebus.Transport.Fake;


namespace Rebus;

public class FakeTransportTests
{
    [Fact]
    public void FakeTransport_CreateQueue_DoesNotThrow()
    {
        var transport = new FakeTransport();
        transport.CreateQueue("queue1");
    }


    [Fact]
    public async Task FakeTransport_GetProperties_ReturnsZeroQueueLength()
    {
        var transport = new FakeTransport();
        var properties = await transport.GetProperties();

        Assert.NotNull(properties);
        Assert.Single(properties);
        Assert.Contains(properties, kvp => kvp.Key == TransportInspectorPropertyKeys.QueueLength && (int)kvp.Value == 0);
    }


    [Fact]
    public void FakeTransport_Address_IsNullWhenConfiguredAsOneWayClient()
    {
        var transport = new FakeTransport();

        Assert.Null(transport.Address);
    }


    [Fact]
    public void FakeTransport_Address_MatchesInputQueueName()
    {
        var transport = new FakeTransport("inputQueue");

        Assert.Equal("inputQueue", transport.Address);
    }


    [Fact]
    public async Task FakeTransport_Receive_AlwaysReturnsNull()
    {
        var transport = new FakeTransport("inputQueue");

        using var scope = new RebusTransactionScope();
        var message = await transport.Receive(scope.TransactionContext, CancellationToken.None);

        Assert.Null(message);
    }


    [Fact]
    public async Task BusUsingFakeTransportAsOneWayClient_Send_DoesNotThrow()
    {
        using var activator = new BuiltinHandlerActivator();
        using var bus = Configure.With(activator)
            .Transport(c => c.UseFakeTransportAsOneWayClient())
            .Routing(c => c.TypeBased().Map<string>("someQueue"))
            .Start();

        await bus.Send("Hey");
    }


    [Fact]
    public async Task BusUsingFakeTransport_Send_DoesNotDeliverMessages()
    {
        var observer = new Observer<string>();

        //Hypothesis that we receive exactly 0 messages
        var hypothesis = Hypothesis.On(observer)
            .Timebox(HypothesisTimeout)
            .Exactly(0)
            .Match(s => true);

        using var activator = new BuiltinHandlerActivator().Register(observer.AsHandler);

        //Initialize bus with a FAKE transport
        using var bus = Configure.With(activator)
            .Transport(c => c.UseFakeTransport("inputQueue"))
            .Start();

        await bus.SendLocal("Saluton mondo");

        await hypothesis.Validate();
    }


    [Fact]
    public async Task BusUsingFakeTransport_PubSub_DoesNotDeliverMessages()
    {
        var observer = new Observer<string>();

        //Hypothesis that we receive exactly 0 messages
        var hypothesis = Hypothesis.On(observer)
            .Timebox(HypothesisTimeout)
            .Exactly(0)
            .Match(s => true);

        using var activator = new BuiltinHandlerActivator().Register(observer.AsHandler);

        //Initialize bus with a FAKE transport but REAL subscription storage
        using var bus = Configure.With(activator)
            .Transport(c => c.UseFakeTransport("inputQueue"))
            .Subscriptions(s => s.StoreInMemory(new InMemorySubscriberStore()))
            .Start();

        await bus.Subscribe<string>();
        await bus.Publish("Saluton mondo");

        await hypothesis.Validate();

        await bus.Unsubscribe<string>();
    }


    [Fact]
    public async Task BusUsingFakeTransport_Defer_DoesNotNeedATimeoutManager()
    {
        using var activator = new BuiltinHandlerActivator();

        //No .Timeouts(...) here on purpose. Rebus's default timeout manager throws, but it is only
        //consulted when a deferred message is RECEIVED. Deferring just stamps headers and sends, so
        //the fake transport discards the message before any timeout manager is involved.
        using var bus = Configure.With(activator)
            .Transport(c => c.UseFakeTransport("inputQueue"))
            .Routing(c => c.TypeBased().Map<string>("someQueue"))
            .Start();

        await bus.Defer(TimeSpan.FromMinutes(1), "Saluton mondo");
        await bus.DeferLocal(TimeSpan.FromMinutes(1), "Saluton mondo");
    }


    [Fact]
    public async Task BusUsingFakeTransport_Defer_DoesNotDeliverMessages()
    {
        var observer = new Observer<string>();

        //Hypothesis that we receive exactly 0 messages
        var hypothesis = Hypothesis.On(observer)
            .Timebox(HypothesisTimeout)
            .Exactly(0)
            .Match(s => true);

        using var activator = new BuiltinHandlerActivator().Register(observer.AsHandler);

        using var bus = Configure.With(activator)
            .Transport(c => c.UseFakeTransport("inputQueue"))
            .Start();

        //Due almost immediately, so a real transport would have delivered it within the timebox
        await bus.DeferLocal(TimeSpan.FromMilliseconds(1), "Saluton mondo");

        await hypothesis.Validate();
    }


    private static readonly TimeSpan HypothesisTimeout = TimeSpan.FromSeconds(0.5);
}
