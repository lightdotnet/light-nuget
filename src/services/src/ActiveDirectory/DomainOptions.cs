namespace Light.ActiveDirectory
{
    public class DomainOptions
    {
        public string Name { get; set; } = "domain.com";

        public bool Enable => !string.IsNullOrEmpty(Name);
    }
}
