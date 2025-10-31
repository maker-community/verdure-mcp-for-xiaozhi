namespace Verdure.McpPlatform.Domain.SeedWork;

/// <summary>
/// Base class for all entities in the domain model
/// Uses Guid Version 7 (time-ordered UUID) for primary keys
/// </summary>
public abstract class Entity
{
    private int? _requestedHashCode;
    private string _id = string.Empty;

    /// <summary>
    /// Gets or sets the entity's unique identifier (Guid Version 7 as string)
    /// </summary>
    public virtual string Id
    {
        get => _id;
        protected set => _id = value;
    }

    /// <summary>
    /// Checks if the entity is transient (not yet persisted)
    /// </summary>
    public bool IsTransient() => string.IsNullOrEmpty(Id);

    /// <summary>
    /// Generates a new Guid Version 7 ID for the entity
    /// Should be called in the constructor for new entities
    /// </summary>
    protected void GenerateId()
    {
        if (IsTransient())
        {
            _id = Guid.CreateVersion7().ToString();
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity entity)
            return false;

        if (ReferenceEquals(this, entity))
            return true;

        if (GetType() != entity.GetType())
            return false;

        if (entity.IsTransient() || IsTransient())
            return false;

        return entity.Id == Id;
    }

    public override int GetHashCode()
    {
        if (!IsTransient())
        {
            if (!_requestedHashCode.HasValue)
                _requestedHashCode = Id.GetHashCode() ^ 31;

            return _requestedHashCode.Value;
        }
        else
        {
            return base.GetHashCode();
        }
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        return left?.Equals(right) ?? Equals(right, null);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }
}
