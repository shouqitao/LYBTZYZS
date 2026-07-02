namespace LYBT.SharedKernel.Primitives;

/// <summary>
/// 实体基类。具有唯一身份标识（Guid）。
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>
    /// 唯一标识
    /// </summary>
    public Guid Id { get; protected init; } = Guid.NewGuid();

    protected Entity() { }

    protected Entity(Guid id) => Id = id;

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);
    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);
    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}


