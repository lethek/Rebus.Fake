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
    [Test]
    public void FakeTransport_CreateQueue_DoesNotThrow()
    {
        var transport = new FakeTransport();
        transport.CreateQueue("queue1");
    }


    [Test]
    public async Task FakeTransport_GetProperties_ReturnsZeroQueueLength()
    {
        var transport = new FakeTransport();
        var properties = await transport.GetProperties();

        await Assert.That(properties).IsNotNull();
        await Assert.That(properties).HasSingleItem();
        await Assert.That(properties[TransportInspectorPropertyKeys.QueueLength]).IsEqualTo(0);
    }


    [Test]
    public async Task FakeTransport_Address_IsNullWhenConfiguredAsOneWayClient()
    {
        var transport = new FakeTransport();

        await Assert.That(transport.Address).IsNull();
    }


    [Test]
    public async Task FakeTransport_Address_MatchesInputQueueName()
    {
        var transport = new FakeTransport("inputQueue");

        await Assert.That(transport.Address).IsEqualTo("inputQueue");
    }


    [Test]
    public async Task FakeTransport_Receive_AlwaysReturnsNull()
    {
        var transport = new FakeTransport("inputQueue");

        using var scope = new RebusTransactionScope();
        var message = await transport.Receive(scope.TransactionContext, CancellationToken.None);

        await Assert.That(message).IsNull();
    }


    [Test]
    public async Task BusUsingFakeTransportAsOneWayClient_Send_DoesNotThrow()
    {
        using var activator = new BuiltinHandlerActivator();
        using var bus = Configure.With(activator)
            .Transport(c => c.UseFakeTransportAsOneWayClient())
            .Routing(c => c.TypeBased().Map<string>("someQueue"))
            .Start();

        await bus.Send("Hey");
    }


    [Test]
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


    [Test]
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


    [Test]
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


    [Test]
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
