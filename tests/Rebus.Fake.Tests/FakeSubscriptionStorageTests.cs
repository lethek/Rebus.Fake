using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;


namespace Rebus;

public class FakeSubscriptionStorageTests
{
    [Fact]
    public async Task BusUsingFakeSubscriptionStorage_Subscribe_DoesNothing()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(c => c.UseFakeTransport("InputQueue"))
            .Subscriptions(c => c.UseFakeSubscriptionStorage())
            .Routing(c => c.TypeBased().Map<string>("SomeQueue"))
            .Start();

        await bus.Subscribe<string>();
        await bus.Unsubscribe<string>();
    }
}
