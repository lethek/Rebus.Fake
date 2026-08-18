using System.Collections.Concurrent;

using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.Fake;
using Rebus.Sagas;
using Rebus.Transport.InMem;


namespace Rebus;

public class FakeSagaStorageTests
{
    [Fact]
    public async Task FakeSagaStorage_Find_AlwaysReturnsNull()
    {
        var storage = new FakeSagaStorage();
        var result = await storage.Find(typeof(TestSagaData), "CorrelationId", Guid.NewGuid());

        Assert.Null(result);
    }


    [Fact]
    public async Task FakeSagaStorage_Insert_DoesNotThrow()
    {
        var storage = new FakeSagaStorage();
        var sagaData = new TestSagaData { Id = Guid.NewGuid(), CorrelationId = Guid.NewGuid() };
        var correlationProperties = new List<ISagaCorrelationProperty>
        {
            new TestCorrelationProperty(nameof(TestSagaData.CorrelationId), sagaData.CorrelationId)
        };

        await storage.Insert(sagaData, correlationProperties);
    }


    [Fact]
    public async Task FakeSagaStorage_Update_DoesNotThrow()
    {
        var storage = new FakeSagaStorage();
        var sagaData = new TestSagaData { Id = Guid.NewGuid(), CorrelationId = Guid.NewGuid() };
        var correlationProperties = new List<ISagaCorrelationProperty>
        {
            new TestCorrelationProperty(nameof(TestSagaData.CorrelationId), sagaData.CorrelationId)
        };

        await storage.Update(sagaData, correlationProperties);
    }


    [Fact]
    public async Task FakeSagaStorage_Delete_DoesNotThrow()
    {
        var storage = new FakeSagaStorage();
        var sagaData = new TestSagaData { Id = Guid.NewGuid(), CorrelationId = Guid.NewGuid() };

        await storage.Delete(sagaData);
    }


    [Fact]
    public async Task FakeSagaStorage_InsertThenFind_StillReturnsNull()
    {
        var storage = new FakeSagaStorage();
        var correlationId = Guid.NewGuid();
        var sagaData = new TestSagaData { Id = Guid.NewGuid(), CorrelationId = correlationId };
        var correlationProperties = new List<ISagaCorrelationProperty>
        {
            new TestCorrelationProperty(nameof(TestSagaData.CorrelationId), correlationId)
        };

        await storage.Insert(sagaData, correlationProperties);

        // Even after insert, Find should return null because it's a fake storage
        var result = await storage.Find(typeof(TestSagaData), nameof(TestSagaData.CorrelationId), correlationId);
        Assert.Null(result);
    }


    [Fact]
    public async Task BusUsingFakeSagaStorage_DoesNotPersistSagaBetweenMessages()
    {
        var observedCounts = new ConcurrentQueue<int>();
        using var handled = new CountdownEvent(2);

        using var activator = new BuiltinHandlerActivator();
        activator.Register(() => new CountingSaga(observedCounts, handled));

        //Initialize bus with a REAL transport but FAKE saga storage
        using var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "sagas"))
            .Sagas(s => s.UseFakeSagaStorage())
            .Start();

        var correlationId = Guid.NewGuid().ToString();
        await bus.SendLocal(new SagaMessage(correlationId));
        await bus.SendLocal(new SagaMessage(correlationId));

        Assert.True(handled.Wait(SagaTimeout), "The saga did not handle both messages within the timeout");

        //Both messages share a correlation id, so real storage would count 1 then 2.
        //FakeSagaStorage.Find always returns null, so every message starts a fresh saga.
        Assert.Equal(2, observedCounts.Count);
        Assert.All(observedCounts, count => Assert.Equal(1, count));
    }


    public record SagaMessage(string CorrelationId);


    public class CountingSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public string CorrelationId { get; set; } = "";
        public int Count { get; set; }
    }


    private class CountingSaga(ConcurrentQueue<int> observedCounts, CountdownEvent handled)
        : Saga<CountingSagaData>, IAmInitiatedBy<SagaMessage>
    {
        protected override void CorrelateMessages(ICorrelationConfig<CountingSagaData> config)
            => config.Correlate<SagaMessage>(m => m.CorrelationId, d => d.CorrelationId);


        public Task Handle(SagaMessage message)
        {
            Data.Count++;
            observedCounts.Enqueue(Data.Count);
            handled.Signal();
            return Task.CompletedTask;
        }
    }


    private static readonly TimeSpan SagaTimeout = TimeSpan.FromSeconds(5);


    private class TestSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public Guid CorrelationId { get; set; }
    }


    private class TestCorrelationProperty(string propertyName, object propertyValue) : ISagaCorrelationProperty
    {
        public string PropertyName { get; } = propertyName;
        public object? PropertyValue { get; } = propertyValue;
        public Type SagaDataType => typeof(TestSagaData);
    }
}
