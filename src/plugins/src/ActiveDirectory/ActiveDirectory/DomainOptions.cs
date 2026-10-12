namespace Light.ActiveDirectory
{
    public class DomainOptions
    {
        /// <summary>
        /// Placeholder default for <see cref="Name"/>. Treated as "not configured" by
        /// <see cref="Services.ActiveDirectoryService.IsConfigured"/>.
        /// </summary>
        internal const string PlaceholderName = "domain.com";

        /// <summary>
        /// The AD domain to connect to. Defaults to the placeholder <c>"domain.com"</c>, which
        /// <see cref="Services.ActiveDirectoryService.IsConfigured"/> reports as not configured.
        /// </summary>
        public string Name { get; set; } = PlaceholderName;

        /// <summary>
        /// <see langword="true"/> when <see cref="Name"/> is non-empty. Note: unlike
        /// <see cref="Services.ActiveDirectoryService.IsConfigured"/>, this does not reject the
        /// <c>"domain.com"</c> placeholder (kept for backward compatibility).
        /// </summary>
        public bool Enable => !string.IsNullOrEmpty(Name);

        internal static bool IsRealDomainName(string? name) =>
            !string.IsNullOrWhiteSpace(name)
            && !string.Equals(name.Trim(), PlaceholderName, StringComparison.OrdinalIgnoreCase);
    }
}
