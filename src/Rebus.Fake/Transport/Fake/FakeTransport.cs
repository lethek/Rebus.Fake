using Rebus.Messages;


namespace Rebus.Transport.Fake;

/// <summary>
/// Implementation of <see cref="ITransport"/> that silently discards all outgoing messages and never receives any.
/// Useful when a Rebus instance must be present but its messaging functionality is not wanted (e.g. offline mode).
/// </summary>
/// <param name="inputQueueName">
/// Name of the transport's input queue, or <c>null</c> to configure the transport as a one-way client.
/// </param>
public class FakeTransport(string? inputQueueName = null)
    : AbstractRebusTransport(inputQueueName), ITransport, ITransportInspector
{
    /// <summary>
    /// Does nothing; no queue is created.
    /// </summary>
    /// <param name="address">Address of the queue that would have been created.</param>
    public override void CreateQueue(string address) { }


    /// <summary>
    /// Always completes with <c>null</c>, because this transport never has any messages to receive.
    /// </summary>
    /// <param name="context">Transaction context of the ongoing receive operation.</param>
    /// <param name="cancellationToken">Token which is cancelled when Rebus shuts down.</param>
    /// <returns>A completed task whose result is always <c>null</c>.</returns>
    public override Task<TransportMessage?> Receive(ITransactionContext context, CancellationToken cancellationToken)
        => Task.FromResult((TransportMessage?)null);


    /// <summary>
    /// Gets properties describing the transport, always reporting a queue length of 0.
    /// </summary>
    /// <param name="cancellationToken">Token which is cancelled when Rebus shuts down.</param>
    /// <returns>A dictionary containing <see cref="TransportInspectorPropertyKeys.QueueLength"/> set to 0.</returns>
    public Task<Dictionary<string, object>> GetProperties(CancellationToken cancellationToken = default)
        => Task.FromResult(
            new Dictionary<string, object> {
                [TransportInspectorPropertyKeys.QueueLength] = 0
            }
        );


    /// <summary>
    /// Silently discards all outgoing messages.
    /// </summary>
    /// <param name="outgoingMessages">Messages which are discarded.</param>
    /// <param name="context">Transaction context of the ongoing send operation.</param>
    /// <returns>A completed task.</returns>
    protected override Task SendOutgoingMessages(IEnumerable<OutgoingTransportMessage> outgoingMessages, ITransactionContext context)
        => Task.CompletedTask;
}
