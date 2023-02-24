using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;

namespace Rebus.NullTransport.Tests;

public class NullTransportTests
{
    [Fact]
    public void NullTransport_CreateQueue_DoesNothing()
    {
        var transport = new NullTransport();
        transport.CreateQueue("queue1");
    }


    [Fact]
    public async Task BusUsingNullTransportAsOneWayClient_Send_DoesNothing()
    {
        using var activator = new BuiltinHandlerActivator();
        
        using var bus = Configure.With(activator)
            .Transport(c => c.UseNullTransportAsOneWayClient())
            .Routing(c => c.TypeBased().Map<string>("SomeQueue"))
            .Start();

        await bus.Send("Hey");
    }


    [Fact]
    public async Task BusUsingNullTransport_Send_DoesNothing()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(c => c.UseNullTransport("InputQueue"))
            .Routing(c => c.TypeBased().Map<string>("SomeQueue"))
            .Start();

        await bus.Send("Hey");
    }
}
