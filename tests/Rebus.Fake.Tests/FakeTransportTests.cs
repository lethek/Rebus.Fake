using Hypothesist;
using Hypothesist.Rebus;

using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
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
        Assert.Contains(properties, kvp => kvp.Key == "queue-length" && (int)kvp.Value == 0);
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


    private static readonly TimeSpan HypothesisTimeout = TimeSpan.FromSeconds(0.5);
}
