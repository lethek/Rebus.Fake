using Rebus.Subscriptions;


namespace Rebus.Persistence.Fake;

/// <summary>
/// Implementation of <see cref="ISubscriptionStorage"/> that never stores any subscriptions and never returns any subscribers.
/// </summary>
public class FakeSubscriptionStorage : ISubscriptionStorage
{
    /// <summary>
    /// Always completes with an empty list, because no subscriptions are ever stored.
    /// </summary>
    /// <param name="topic">Topic to get subscriber addresses for.</param>
    /// <returns>A completed task whose result is always an empty list.</returns>
    public Task<IReadOnlyList<string>> GetSubscriberAddresses(string topic)
        => Task.FromResult(NoSubscriberAddresses);


    /// <summary>
    /// Does nothing; the subscription is discarded.
    /// </summary>
    /// <param name="topic">Topic which is ignored.</param>
    /// <param name="subscriberAddress">Subscriber address which is ignored.</param>
    /// <returns>A completed task.</returns>
    public Task RegisterSubscriber(string topic, string subscriberAddress)
        => Task.CompletedTask;


    /// <summary>
    /// Does nothing; there are never any stored subscriptions to remove.
    /// </summary>
    /// <param name="topic">Topic which is ignored.</param>
    /// <param name="subscriberAddress">Subscriber address which is ignored.</param>
    /// <returns>A completed task.</returns>
    public Task UnregisterSubscriber(string topic, string subscriberAddress)
        => Task.CompletedTask;


    /// <summary>
    /// Always <c>true</c>: this storage is treated as centralized, so subscribers register themselves directly.
    /// </summary>
    public bool IsCentralized { get; } = true;


    private static readonly IReadOnlyList<string> NoSubscriberAddresses = [];
}
