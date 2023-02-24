using Rebus.Config;
using Rebus.Subscriptions;
using Rebus.Transport;


namespace Rebus.NullTransport;

public static class StandardConfigurerExtensions
{
    /// <summary>
    /// Configures Rebus to use no-op message queues, delivering/receiving is an empty operation
    /// </summary>
    public static void UseNullTransport(this StandardConfigurer<ITransport> configurer, string inputQueueName)
    {
        if (configurer == null) {
            throw new ArgumentNullException(nameof(configurer));
        }

        if (inputQueueName == null) {
            throw new ArgumentNullException(nameof(inputQueueName));
        }

        configurer
            .OtherService<NullTransport>()
            .Register(c => new NullTransport(inputQueueName));

        configurer
            .OtherService<ITransportInspector>()
            .Register(c => c.Get<NullTransport>());

        configurer.Register(c => c.Get<NullTransport>());
    }


    /// <summary>
    /// Configures Rebus to use no-op message queues, configuring this instance to be a one-way client, delivering is an empty operation
    /// </summary>
    public static void UseNullTransportAsOneWayClient(this StandardConfigurer<ITransport> configurer)
    {
        if (configurer == null) {
            throw new ArgumentNullException(nameof(configurer));
        }

        configurer.Register(c => new NullTransport());

        OneWayClientBackdoor.ConfigureOneWayClient(configurer);
    }


    public static void UseNullSubscriptionStorage(this StandardConfigurer<ISubscriptionStorage> configurer)
    {
        if (configurer == null) {
            throw new ArgumentNullException(nameof(configurer));
        }

        configurer.Register(c => new NullSubscriptionStorage());
    }
}
