using CsvHelper.Configuration;

namespace Light.File.Csv
{
    /// <summary>
    /// Options for configuring the CSV service, e.g. <c>new CsvService { InjectionOptions = o.CsvInjectionOptions }</c>.
    /// </summary>
    public class CsvFileGeneratorOptions
    {
        /// <summary>
        /// Value assigned to <see cref="CsvService.InjectionOptions"/> of the registered <see cref="CsvService"/>.
        /// Defaults to <see cref="InjectionOptions.Escape"/>; use <see cref="InjectionOptions.None"/> to opt out of
        /// CSV/formula-injection escaping.
        /// </summary>
        public InjectionOptions CsvInjectionOptions { get; set; } = InjectionOptions.Escape;
    }
}
