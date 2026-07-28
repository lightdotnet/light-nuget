namespace Light.Domain;

public static class LightId
{
    public static string NewId() => Ulid.NewUlid().ToString();
}
