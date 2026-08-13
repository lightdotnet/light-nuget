namespace Light.Domain;

public readonly struct LightId : IEquatable<LightId>, IComparable<LightId>
{
    public static readonly LightId Empty = default;

    public LightId()
    {
        Guid = Guid.CreateVersion7();
    }

    public LightId(Guid value)
    {
        Guid = value;
    }

    public Guid Guid { get; }

    public override string ToString() => Guid.ToString();

    public override bool Equals(object? obj) => obj is LightId other && Equals(other);

    public bool Equals(LightId other) => Guid.Equals(other.Guid);

    public override int GetHashCode() => Guid.GetHashCode();

    public int CompareTo(LightId other) => Guid.CompareTo(other.Guid);

    public static bool operator ==(LightId left, LightId right) => left.Equals(right);

    public static bool operator !=(LightId left, LightId right) => !left.Equals(right);

    public static implicit operator string(LightId lightId)
        => lightId.Guid.ToString();

    public static implicit operator Guid(LightId lightId)
        => lightId.Guid;

    public static LightId NewId() => new();
}
