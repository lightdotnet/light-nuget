using Light.File.Csv;
using Light.File.Excel;
using Light.Infrastructure.Csv;
using Light.Infrastructure.Excel;
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