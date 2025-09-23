using Rebus.Subscriptions;


namespace Rebus.Persistence.Fake;

public class FakeSubscriptionStorage : ISubscriptionStorage
{
    public Task<IReadOnlyList<string>> GetSubscriberAddresses(string topic)
        => Task.FromResult(NoSubscriberAddresses);


    public Task RegisterSubscriber(string topic, string subscriberAddress)
        => Task.CompletedTask;


    public Task UnregisterSubscriber(string topic, string subscriberAddress)
        => Task.CompletedTask;


    public bool IsCentralized { get; } = true;


    private static readonly IReadOnlyList<string> NoSubscriberAddresses = [];
}
