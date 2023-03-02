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
        //Hypothesis that we receive exactly 0 messages
        var hypothesis = Hypothesis.For<string>()
            .Exactly(0, s => true);

        using var activator = new BuiltinHandlerActivator().Register(hypothesis.AsHandler);

        //Initialize bus with a FAKE transport
        using var bus = Configure.With(activator)
            .Transport(c => c.UseFakeTransport("inputQueue"))
            .Start();

        await bus.SendLocal("Saluton mondo");

        await hypothesis.Validate(HypothesisTimeout);
    }


    [Fact]
    public async Task BusUsingFakeTransport_PubSub_DoesNotDeliverMessages()
    {
        //Hypothesis that we receive exactly 0 messages
        var hypothesis = Hypothesis.For<string>()
            .Exactly(0, s => true);

        using var activator = new BuiltinHandlerActivator().Register(hypothesis.AsHandler);

        //Initialize bus with a FAKE transport but REAL subscription storage
        using var bus = Configure.With(activator)
            .Transport(c => c.UseFakeTransport("inputQueue"))
            .Subscriptions(s => s.StoreInMemory(new InMemorySubscriberStore()))
            .Start();

        await bus.Subscribe<string>();
        await bus.Publish("Saluton mondo");

        await hypothesis.Validate(HypothesisTimeout);

        await bus.Unsubscribe<string>();
    }


    private static readonly TimeSpan HypothesisTimeout = TimeSpan.FromSeconds(0.5);
}
