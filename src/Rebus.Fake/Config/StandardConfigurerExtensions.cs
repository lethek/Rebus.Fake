using Rebus.Persistence.Fake;
using Rebus.Sagas;
using Rebus.Subscriptions;
using Rebus.Transport;
using Rebus.Transport.Fake;


namespace Rebus.Config;

public static class StandardConfigurerExtensions
{
    /// <summary>
    /// Configures Rebus to use no-op message queues, delivering/receiving is an empty operation
    /// </summary>
    public static void UseFakeTransport(this StandardConfigurer<ITransport> configurer, string inputQueueName)
    {
        if (configurer == null) {
            throw new ArgumentNullException(nameof(configurer));
        }

        if (inputQueueName == null) {
            throw new ArgumentNullException(nameof(inputQueueName));
        }

        configurer
            .OtherService<FakeTransport>()
            .Register(c => new FakeTransport(inputQueueName));

        configurer
            .OtherService<ITransportInspector>()
            .Register(c => c.Get<FakeTransport>());

        configurer.Register(c => c.Get<FakeTransport>());
    }


    /// <summary>
    /// Configures Rebus to use no-op message queues, configuring this instance to be a one-way client, delivering is an empty operation
    /// </summary>
    public static void UseFakeTransportAsOneWayClient(this StandardConfigurer<ITransport> configurer)
    {
        if (configurer == null) {
            throw new ArgumentNullException(nameof(configurer));
        }

        configurer.Register(c => new FakeTransport());

        OneWayClientBackdoor.ConfigureOneWayClient(configurer);
    }


    public static void UseFakeSubscriptionStorage(this StandardConfigurer<ISubscriptionStorage> configurer)
    {
        if (configurer == null) {
            throw new ArgumentNullException(nameof(configurer));
        }

        configurer.Register(c => new FakeSubscriptionStorage());
    }


    public static void UseFakeSagaStorage(this StandardConfigurer<ISagaStorage> configurer)
    {
        if (configurer == null) {
            throw new ArgumentNullException(nameof(configurer));
        }

        configurer.Register(c => new FakeSagaStorage());
    }
}
