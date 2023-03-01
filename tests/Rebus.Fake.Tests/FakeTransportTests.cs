using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Transport.Fake;


namespace Rebus;

public class FakeTransportTests
{
    [Fact]
    public void FakeTransport_CreateQueue_DoesNothing()
    {
        var transport = new FakeTransport();
        transport.CreateQueue("queue1");
    }


    [Fact]
    public async Task BusUsingFakeTransportAsOneWayClient_Send_DoesNothing()
    {
        using var activator = new BuiltinHandlerActivator();
        
        using var bus = Configure.With(activator)
            .Transport(c => c.UseFakeTransportAsOneWayClient())
            .Routing(c => c.TypeBased().Map<string>("SomeQueue"))
            .Start();

        await bus.Send("Hey");
    }


    [Fact]
    public async Task BusUsingFakeTransport_Send_DoesNothing()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(c => c.UseFakeTransport("InputQueue"))
            .Routing(c => c.TypeBased().Map<string>("SomeQueue"))
            .Start();

        await bus.Send("Hey");
    }
}
