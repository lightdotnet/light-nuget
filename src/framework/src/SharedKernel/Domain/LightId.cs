namespace Light.Domain;

public struct LightId
{
    public static string NewId() => Ulid.NewUlid().ToString();
}
