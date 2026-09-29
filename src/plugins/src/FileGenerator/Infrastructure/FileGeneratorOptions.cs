using CsvHelper.Configuration;
using Light.Infrastructure.Csv;

namespace Light.Infrastructure
{
    /// <summary>
    /// Options for <c>AddFileGenerator(Action&lt;FileGeneratorOptions&gt;)</c>.
    /// </summary>
    public class FileGeneratorOptions
    {
        /// <summary>
        /// Value assigned to <see cref="CsvService.InjectionOptions"/> of the registered <see cref="CsvService"/>.
        /// Defaults to <see cref="InjectionOptions.Escape"/>; use <see cref="InjectionOptions.None"/> to opt out of
        /// CSV/formula-injection escaping.
        /// </summary>
        public InjectionOptions CsvInjectionOptions { get; set; } = InjectionOptions.Escape;
    }
}
