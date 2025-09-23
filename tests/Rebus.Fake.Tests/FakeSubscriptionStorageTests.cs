using Hypothesist;
using Hypothesist.Rebus;

using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.Fake;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;


namespace Rebus;

public class FakeSubscriptionStorageTests
{
    [Fact]
    public async Task FakeSubscriptionStorage_GetSubscriberAddresses_IsAlwaysEmpty()
    {
        var storage = new FakeSubscriptionStorage();
        await storage.RegisterSubscriber("topic", "subscriber");
        Assert.Empty(await storage.GetSubscriberAddresses("topic"));
    }


    [Fact]
    public async Task FakeSubscriptionStorage_RegisterAndUnregister_DoesNotThrow()
    {
        var storage = new FakeSubscriptionStorage();
        await storage.RegisterSubscriber("topic", "subscriber");
        await storage.UnregisterSubscriber("topic", "subscriber");
    }


    [Fact]
    public async Task BusUsingFakeSubscriptionStorage_SubscribeAndUnsubscribe_DoesNotThrow()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(c => c.UseFakeTransport("inputQueue"))
            .Subscriptions(c => c.UseFakeSubscriptionStorage())
            .Routing(c => c.TypeBased().Map<string>("someQueue"))
            .Start();

        await bus.Subscribe<string>();
        await bus.Unsubscribe<string>();
    }


    [Fact]
    public async Task BusUsingFakeSubscriptionStorage_PubSub_DoesNotReceiveMessages()
    {
        var observer = new Observer<string>();

        //Hypothesis that we receive exactly 0 messages
        var hypothesis = Hypothesis.On(observer)
            .Timebox(HypothesisTimeout)
            .Exactly(0)
            .Match(s => true);

        using var activator = new BuiltinHandlerActivator().Register(observer.AsHandler);

        //Initialize bus with a REAL transport but FAKE subscription storage
        using var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "subscriber", registerSubscriptionStorage: false))
            .Subscriptions(c => c.UseFakeSubscriptionStorage())
            .Start();

        await bus.Subscribe<string>();
        await bus.Publish("Saluton mondo");

        await hypothesis.Validate();

        await bus.Unsubscribe<string>();
    }
    

    private static readonly TimeSpan HypothesisTimeout = TimeSpan.FromSeconds(0.5);
}
