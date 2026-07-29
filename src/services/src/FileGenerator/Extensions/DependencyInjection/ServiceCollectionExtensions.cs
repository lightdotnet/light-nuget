using Light.FileGenerator.File.Csv;
using Light.FileGenerator.File.Excel;
using Light.FileGenerator.Infrastructure.Csv;
using Light.FileGenerator.Infrastructure.Excel;
using Microsoft.Extensions.DependencyInjection;

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
    }
}