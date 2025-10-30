namespace Verdure.McpPlatform.Domain.SeedWork;

/// <summary>
/// Base repository interface for all aggregate roots
/// </summary>
/// <typeparam name="T">The aggregate root type</typeparam>
public interface IRepository<T> where T : IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }
}
