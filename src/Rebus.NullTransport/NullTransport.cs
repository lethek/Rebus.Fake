using Rebus.Messages;
using Rebus.Transport;


namespace Rebus.NullTransport;

public class NullTransport : AbstractRebusTransport, ITransport, ITransportInspector
{
    public NullTransport(string? inputQueueName = null) : base(inputQueueName) { }


    public override void CreateQueue(string address) { }


    public override Task<TransportMessage?> Receive(ITransactionContext context, CancellationToken cancellationToken)
        => NoTransportMessageTask;


    public Task<Dictionary<string, object>> GetProperties(CancellationToken cancellationToken = default)
        => EmptyPropertiesTask;


    protected override Task SendOutgoingMessages(IEnumerable<OutgoingMessage> outgoingMessages, ITransactionContext context)
        => Task.CompletedTask;


    private static readonly Task<TransportMessage?> NoTransportMessageTask
        = Task.FromResult((TransportMessage?)null);


    private static readonly Task<Dictionary<string, object>> EmptyPropertiesTask
        = Task.FromResult(
            new Dictionary<string, object> {
                { TransportInspectorPropertyKeys.QueueLength, 0 }
            }
        );
}
