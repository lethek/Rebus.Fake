using Hypothesist;

using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.Fake;
using Rebus.Sagas;
using Rebus.Transport.InMem;


namespace Rebus;

public class FakeSagaStorageTests
{
    [Test]
    public async Task FakeSagaStorage_Find_AlwaysReturnsNull()
    {
        var storage = new FakeSagaStorage();
        var result = await storage.Find(typeof(TestSagaData), "CorrelationId", Guid.NewGuid());

        await Assert.That(result).IsNull();
    }


    [Test]
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


    [Test]
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


    [Test]
    public async Task FakeSagaStorage_Delete_DoesNotThrow()
    {
        var storage = new FakeSagaStorage();
        var sagaData = new TestSagaData { Id = Guid.NewGuid(), CorrelationId = Guid.NewGuid() };

        await storage.Delete(sagaData);
    }


    [Test]
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
        await Assert.That(result).IsNull();
    }


    [Test]
    public async Task BusUsingFakeSagaStorage_DoesNotPersistSagaBetweenMessages()
    {
        var observer = new Observer<int>();

        //Hypothesis that 2 messages are each handled by a fresh saga, i.e. both see a count of 1.
        //AtLeast completes as soon as the second match arrives, so a passing run never waits out the timebox.
        var hypothesis = Hypothesis.On(observer)
            .Timebox(SagaTimeout)
            .AtLeast(2)
            .Match(count => count == 1);

        using var activator = new BuiltinHandlerActivator();
        activator.Register(() => new CountingSaga(observer));

        //Initialize bus with a REAL transport but FAKE saga storage
        using var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "sagas"))
            .Sagas(s => s.UseFakeSagaStorage())
            .Start();

        var correlationId = Guid.NewGuid().ToString();
        await bus.SendLocal(new SagaMessage(correlationId));
        await bus.SendLocal(new SagaMessage(correlationId));

        //Both messages share a correlation id, so real storage would count 1 then 2.
        //FakeSagaStorage.Find always returns null, so every message starts a fresh saga.
        await hypothesis.Validate();
    }


    public record SagaMessage(string CorrelationId);


    public class CountingSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public string CorrelationId { get; set; } = "";
        public int Count { get; set; }
    }


    private class CountingSaga(Observer<int> observer)
        : Saga<CountingSagaData>, IAmInitiatedBy<SagaMessage>
    {
        protected override void CorrelateMessages(ICorrelationConfig<CountingSagaData> config)
            => config.Correlate<SagaMessage>(m => m.CorrelationId, d => d.CorrelationId);


        public async Task Handle(SagaMessage message)
        {
            Data.Count++;
            await observer.Add(Data.Count, CancellationToken.None);
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
