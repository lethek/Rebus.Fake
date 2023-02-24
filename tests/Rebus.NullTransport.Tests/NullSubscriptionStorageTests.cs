using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;

namespace Rebus.NullTransport.Tests;

public class NullSubscriptionStorageTests
{
    [Fact]
    public async Task BusUsingNullSubscriptionStorage_Subscribe_DoesNothing()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(c => c.UseNullTransport("InputQueue"))
            .Subscriptions(c => c.UseNullSubscriptionStorage())
            .Routing(c => c.TypeBased().Map<string>("SomeQueue"))
            .Start();

        await bus.Subscribe<string>();
        await bus.Unsubscribe<string>();
    }
}
