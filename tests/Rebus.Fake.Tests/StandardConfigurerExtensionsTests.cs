using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Sagas;
using Rebus.Subscriptions;
using Rebus.Transport;


namespace Rebus;

public class StandardConfigurerExtensionsTests
{
    [Test]
    public async Task UseFakeTransport_WithNullConfigurer_ThrowsArgumentNullException()
    {
        StandardConfigurer<ITransport> configurer = null!;

        var ex = Assert.Throws<ArgumentNullException>(() => configurer.UseFakeTransport("inputQueue"));

        await Assert.That(ex.ParamName).IsEqualTo("configurer");
    }


    [Test]
    public async Task UseFakeTransport_WithNullInputQueueName_ThrowsArgumentNullException()
    {
        using var activator = new BuiltinHandlerActivator();
        var ex = Assert.Throws<ArgumentNullException>(() =>
        {
            Configure.With(activator)
                .Transport(t => t.UseFakeTransport(null!));
        });

        await Assert.That(ex.ParamName).IsEqualTo("inputQueueName");
    }


    [Test]
    public async Task UseFakeTransport_WithValidInputQueueName_ConfiguresBusSuccessfully()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Start();

        await Assert.That(bus).IsNotNull();
    }


    [Test]
    public async Task UseFakeTransport_AllowsSendingMessages()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Start();

        // Should not throw
        await bus.SendLocal("test message");
    }


    [Test]
    public async Task UseFakeTransport_RegistersTransportInspector()
    {
        ITransportInspector? inspector = null;

        using var activator = new BuiltinHandlerActivator();

        //Nothing resolves ITransportInspector during a normal Start, so resolve it from an IBus
        //decorator, which does run. This covers the registration that UseFakeTransport sets up.
        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Options(o => o.Decorate<IBus>(c =>
            {
                inspector = c.Get<ITransportInspector>();
                return c.Get<IBus>();
            }))
            .Start();

        await Assert.That(inspector).IsNotNull();

        var properties = await inspector!.GetProperties(CancellationToken.None);
        await Assert.That(properties[TransportInspectorPropertyKeys.QueueLength]).IsEqualTo(0);
    }


    [Test]
    public async Task UseFakeTransportAsOneWayClient_WithNullConfigurer_ThrowsArgumentNullException()
    {
        StandardConfigurer<ITransport> configurer = null!;

        var ex = Assert.Throws<ArgumentNullException>(() => configurer.UseFakeTransportAsOneWayClient());

        await Assert.That(ex.ParamName).IsEqualTo("configurer");
    }


    [Test]
    public async Task UseFakeTransportAsOneWayClient_ConfiguresBusSuccessfully()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransportAsOneWayClient())
            .Routing(r => r.TypeBased().Map<string>("dummy-queue"))
            .Start();

        await Assert.That(bus).IsNotNull();
    }


    [Test]
    public async Task UseFakeTransportAsOneWayClient_AllowsSendingMessages()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransportAsOneWayClient())
            .Routing(r => r.TypeBased().Map<string>("dummy-queue"))
            .Start();

        // Should not throw
        await bus.Send("test message");
    }


    [Test]
    public async Task UseFakeSubscriptionStorage_WithNullConfigurer_ThrowsArgumentNullException()
    {
        StandardConfigurer<ISubscriptionStorage> configurer = null!;

        var ex = Assert.Throws<ArgumentNullException>(() => configurer.UseFakeSubscriptionStorage());

        await Assert.That(ex.ParamName).IsEqualTo("configurer");
    }


    [Test]
    public async Task UseFakeSubscriptionStorage_ConfiguresBusSuccessfully()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Subscriptions(s => s.UseFakeSubscriptionStorage())
            .Start();

        await Assert.That(bus).IsNotNull();
    }


    [Test]
    public async Task UseFakeSubscriptionStorage_AllowsSubscribeAndUnsubscribe()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Subscriptions(s => s.UseFakeSubscriptionStorage())
            .Start();

        // Should not throw
        await bus.Subscribe<string>();
        await bus.Unsubscribe<string>();
    }


    [Test]
    public async Task UseFakeSagaStorage_WithNullConfigurer_ThrowsArgumentNullException()
    {
        StandardConfigurer<ISagaStorage> configurer = null!;

        var ex = Assert.Throws<ArgumentNullException>(() => configurer.UseFakeSagaStorage());

        await Assert.That(ex.ParamName).IsEqualTo("configurer");
    }


    [Test]
    public async Task UseFakeSagaStorage_ConfiguresBusSuccessfully()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Sagas(s => s.UseFakeSagaStorage())
            .Start();

        await Assert.That(bus).IsNotNull();
    }


    [Test]
    public async Task AllFakeComponents_CanBeConfiguredTogether()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Subscriptions(s => s.UseFakeSubscriptionStorage())
            .Sagas(s => s.UseFakeSagaStorage())
            .Start();

        await Assert.That(bus).IsNotNull();
    }


    [Test]
    public async Task AllFakeComponents_WorkTogetherWithoutErrors()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Subscriptions(s => s.UseFakeSubscriptionStorage())
            .Sagas(s => s.UseFakeSagaStorage())
            .Start();

        // Should all complete without throwing
        await bus.Subscribe<string>();
        await bus.SendLocal("test message");
        await bus.Publish("test event");
        await bus.DeferLocal(TimeSpan.FromMinutes(1), "deferred message");
        await bus.Unsubscribe<string>();
    }
}
