using Rebus.Messages;


namespace Rebus.Transport.Fake;

public class FakeTransport(string? inputQueueName = null)
    : AbstractRebusTransport(inputQueueName), ITransport, ITransportInspector
{
    public override void CreateQueue(string address) { }


    public override Task<TransportMessage?> Receive(ITransactionContext context, CancellationToken cancellationToken)
        => Task.FromResult((TransportMessage?)null);


    public Task<Dictionary<string, object>> GetProperties(CancellationToken cancellationToken = default)
        => Task.FromResult(
            new Dictionary<string, object> {
                [TransportInspectorPropertyKeys.QueueLength] = 0
            }
        );


    protected override Task SendOutgoingMessages(IEnumerable<OutgoingTransportMessage> outgoingMessages, ITransactionContext context)
        => Task.CompletedTask;
}
