using Rebus.Sagas;


namespace Rebus.Persistence.Fake;

/// <summary>
/// Implementation of <see cref="ISagaStorage"/> that never stores anything and never finds any saga data.
/// </summary>
public class FakeSagaStorage : ISagaStorage
{
    /// <summary>
    /// Always completes with <c>null</c>, because no saga data is ever stored.
    /// </summary>
    /// <param name="sagaDataType">Type of saga data to look for.</param>
    /// <param name="propertyName">Name of the correlation property to match.</param>
    /// <param name="propertyValue">Value the correlation property is matched against.</param>
    /// <returns>A completed task whose result is always <c>null</c>.</returns>
    public Task<ISagaData?> Find(Type sagaDataType, string propertyName, object propertyValue)
        => Task.FromResult((ISagaData?)null);


    /// <summary>
    /// Does nothing; the saga data is discarded.
    /// </summary>
    /// <param name="sagaData">Saga data which is discarded.</param>
    /// <param name="correlationProperties">Correlation properties which are ignored.</param>
    /// <returns>A completed task.</returns>
    public Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        => Task.CompletedTask;


    /// <summary>
    /// Does nothing; the saga data is discarded.
    /// </summary>
    /// <param name="sagaData">Saga data which is discarded.</param>
    /// <param name="correlationProperties">Correlation properties which are ignored.</param>
    /// <returns>A completed task.</returns>
    public Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        => Task.CompletedTask;


    /// <summary>
    /// Does nothing; there is never any stored saga data to delete.
    /// </summary>
    /// <param name="sagaData">Saga data which is ignored.</param>
    /// <returns>A completed task.</returns>
    public Task Delete(ISagaData sagaData)
        => Task.CompletedTask;
}
