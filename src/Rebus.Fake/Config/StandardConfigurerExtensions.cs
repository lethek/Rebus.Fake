using Rebus.Persistence.Fake;
using Rebus.Sagas;
using Rebus.Subscriptions;
using Rebus.Transport;
using Rebus.Transport.Fake;


namespace Rebus.Config;

/// <summary>
/// Configuration extensions for registering the fake, no-op Rebus components.
/// </summary>
public static class StandardConfigurerExtensions
{
    /// <summary>
    /// Configures Rebus to use no-op message queues, delivering/receiving is an empty operation
    /// </summary>
    /// <param name="configurer">Transport configurer to register the fake transport with.</param>
    /// <param name="inputQueueName">Name of the bus's input queue.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurer"/> or <paramref name="inputQueueName"/> is <c>null</c>.</exception>
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
    /// <param name="configurer">Transport configurer to register the fake transport with.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurer"/> is <c>null</c>.</exception>
    public static void UseFakeTransportAsOneWayClient(this StandardConfigurer<ITransport> configurer)
    {
        if (configurer == null) {
            throw new ArgumentNullException(nameof(configurer));
        }

        configurer.Register(c => new FakeTransport());

        OneWayClientBackdoor.ConfigureOneWayClient(configurer);
    }


    /// <summary>
    /// Configures Rebus to use no-op subscription storage, where subscriptions are discarded and no subscribers are ever found
    /// </summary>
    /// <param name="configurer">Subscription storage configurer to register the fake storage with.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurer"/> is <c>null</c>.</exception>
    public static void UseFakeSubscriptionStorage(this StandardConfigurer<ISubscriptionStorage> configurer)
    {
        if (configurer == null) {
            throw new ArgumentNullException(nameof(configurer));
        }

        configurer.Register(c => new FakeSubscriptionStorage());
    }


    /// <summary>
    /// Configures Rebus to use no-op saga storage, where saga data is discarded and no sagas are ever found
    /// </summary>
    /// <param name="configurer">Saga storage configurer to register the fake storage with.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurer"/> is <c>null</c>.</exception>
    public static void UseFakeSagaStorage(this StandardConfigurer<ISagaStorage> configurer)
    {
        if (configurer == null) {
            throw new ArgumentNullException(nameof(configurer));
        }

        configurer.Register(c => new FakeSagaStorage());
    }
}
