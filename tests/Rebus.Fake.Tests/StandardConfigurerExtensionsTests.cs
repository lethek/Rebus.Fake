using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Sagas;
using Rebus.Subscriptions;
using Rebus.Transport;


namespace Rebus;

public class StandardConfigurerExtensionsTests
{
    [Fact]
    public void UseFakeTransport_WithNullConfigurer_ThrowsArgumentNullException()
    {
        StandardConfigurer<ITransport> configurer = null!;

        var ex = Assert.Throws<ArgumentNullException>(() => configurer.UseFakeTransport("inputQueue"));

        Assert.Equal("configurer", ex.ParamName);
    }


    [Fact]
    public void UseFakeTransport_WithNullInputQueueName_ThrowsArgumentNullException()
    {
        using var activator = new BuiltinHandlerActivator();
        var ex = Assert.Throws<ArgumentNullException>(() =>
        {
            Configure.With(activator)
                .Transport(t => t.UseFakeTransport(null!));
        });

        Assert.Equal("inputQueueName", ex.ParamName);
    }


    [Fact]
    public void UseFakeTransport_WithValidInputQueueName_ConfiguresBusSuccessfully()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Start();

        Assert.NotNull(bus);
    }


    [Fact]
    public async Task UseFakeTransport_AllowsSendingMessages()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Start();

        // Should not throw
        await bus.SendLocal("test message");
    }


    [Fact]
    public void UseFakeTransportAsOneWayClient_WithNullConfigurer_ThrowsArgumentNullException()
    {
        StandardConfigurer<ITransport> configurer = null!;

        var ex = Assert.Throws<ArgumentNullException>(() => configurer.UseFakeTransportAsOneWayClient());

        Assert.Equal("configurer", ex.ParamName);
    }


    [Fact]
    public void UseFakeTransportAsOneWayClient_ConfiguresBusSuccessfully()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransportAsOneWayClient())
            .Routing(r => r.TypeBased().Map<string>("dummy-queue"))
            .Start();

        Assert.NotNull(bus);
    }


    [Fact]
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


    [Fact]
    public void UseFakeSubscriptionStorage_WithNullConfigurer_ThrowsArgumentNullException()
    {
        StandardConfigurer<ISubscriptionStorage> configurer = null!;

        var ex = Assert.Throws<ArgumentNullException>(() => configurer.UseFakeSubscriptionStorage());

        Assert.Equal("configurer", ex.ParamName);
    }


    [Fact]
    public void UseFakeSubscriptionStorage_ConfiguresBusSuccessfully()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Subscriptions(s => s.UseFakeSubscriptionStorage())
            .Start();

        Assert.NotNull(bus);
    }


    [Fact]
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


    [Fact]
    public void UseFakeSagaStorage_WithNullConfigurer_ThrowsArgumentNullException()
    {
        StandardConfigurer<ISagaStorage> configurer = null!;

        var ex = Assert.Throws<ArgumentNullException>(() => configurer.UseFakeSagaStorage());

        Assert.Equal("configurer", ex.ParamName);
    }


    [Fact]
    public void UseFakeSagaStorage_ConfiguresBusSuccessfully()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Sagas(s => s.UseFakeSagaStorage())
            .Start();

        Assert.NotNull(bus);
    }


    [Fact]
    public void AllFakeComponents_CanBeConfiguredTogether()
    {
        using var activator = new BuiltinHandlerActivator();

        using var bus = Configure.With(activator)
            .Transport(t => t.UseFakeTransport("test-queue"))
            .Subscriptions(s => s.UseFakeSubscriptionStorage())
            .Sagas(s => s.UseFakeSagaStorage())
            .Start();

        Assert.NotNull(bus);
    }


    [Fact]
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
