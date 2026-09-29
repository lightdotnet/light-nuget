using Light.File.Csv;
using Light.File.Excel;
using Light.Infrastructure;
using Light.Infrastructure.Csv;
using Light.Infrastructure.Excel;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Light.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileGenerator(this IServiceCollection services)
        {
            services.AddTransient<IExcelService, ExcelService>();
            services.AddTransient<ICsvService, CsvService>();

            return services;
        }

        /// <summary>
        /// Registers <see cref="IExcelService"/> and <see cref="ICsvService"/> (transient) like <see cref="AddFileGenerator(IServiceCollection)"/>,
        /// with the <see cref="CsvService"/> configured from <paramref name="configure"/>
        /// (e.g. <c>o =&gt; o.CsvInjectionOptions = InjectionOptions.None</c> to opt out of formula-injection escaping).
        /// </summary>
        public static IServiceCollection AddFileGenerator(this IServiceCollection services, Action<FileGeneratorOptions> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            var options = new FileGeneratorOptions();
            configure(options);

            var injectionOptions = options.CsvInjectionOptions;

            services.AddTransient<IExcelService, ExcelService>();
            services.AddTransient<ICsvService>(_ => new CsvService { InjectionOptions = injectionOptions });

            return services;
        }
    }
}
