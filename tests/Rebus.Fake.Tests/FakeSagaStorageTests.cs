using Rebus.Persistence.Fake;
using Rebus.Sagas;


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
