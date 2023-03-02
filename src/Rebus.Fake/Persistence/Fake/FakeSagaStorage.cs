using Rebus.Sagas;


namespace Rebus.Persistence.Fake;

public class FakeSagaStorage : ISagaStorage
{
    public Task<ISagaData?> Find(Type sagaDataType, string propertyName, object propertyValue)
        => Task.FromResult((ISagaData?)null);


    public Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        => Task.CompletedTask;


    public Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        => Task.CompletedTask;


    public Task Delete(ISagaData sagaData)
        => Task.CompletedTask;
}
