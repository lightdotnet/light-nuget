using Elastic.CommonSchema.Serilog;
using Elastic.Ingest.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;

namespace Light.Serilog
{
    public static class SerilogConfigurationExtensions
    {
        private static LoggerConfiguration BaseConfig(this LoggerConfiguration logger)
        {
            logger
                //.Filter.ByExcluding(x => x.MessageTemplate.Text.Contains("Executing endpoint"))
                //.MinimumLevel.Information()
                //.MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
                //.MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                //.MinimumLevel.Override("System", LogEventLevel.Information)
                //.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                //.MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .WriteTo.Async(c => c.Debug())
                .WriteTo.Async(c => c.Console());

            return logger;
        }

        private static LoggerConfiguration WriteToFile(this LoggerConfiguration logger, IConfiguration configuration, string applicationName, string environment)
        {
            var elementName = "FileAsync";

            var options = SerilogOptionsExtensions.GetWriteTo(configuration, elementName);

            if (options == null)
            {
                return logger;
            }

            // custom settings
            string? configPath = null;
            string? configTemplate = null;

            // get options values
            if (options.Args != null)
            {
                options.Args.TryGetValue("Path", out configPath);
                options.Args.TryGetValue("Template", out configTemplate);
            }

            // default settings
            var defaultPath = "logs"; // default save logs files to [current directory]/logs
            var defaultTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{SourceContext}{NewLine}{Exception}";

            //var template = "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";
            //var template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}";
            var template = !string.IsNullOrEmpty(configTemplate) ? configTemplate : defaultTemplate;
            var path = !string.IsNullOrEmpty(configPath) ? configPath : defaultPath;

            var limitFileSize = 52428800; // 50mb to bytes

            logger.WriteTo.Async(c => c.File(Path.Combine(path, $"{applicationName}-{environment}-log-.txt"),
                outputTemplate: template,
                shared: true,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: limitFileSize));

            return logger;
        }

        private static LoggerConfiguration WriteToElasticsearch(this LoggerConfiguration logger, IConfiguration configuration, string applicationName, string environment)
        {
            var elementName = "ElasticsearchAsync";

            var options = SerilogOptionsExtensions.GetWriteTo(configuration, elementName);

            if (options != null && options.Args != null)
            {
                // get Elasticsearch configuration in WriteTo Args
                options.Args.TryGetValue("ServiceName", out string? serviceName);
                options.Args.TryGetValue("Endpoint", out string? endpoint);
                options.Args.TryGetValue("Username", out string? username);
                options.Args.TryGetValue("Password", out string? password);

                if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    if (string.IsNullOrEmpty(serviceName))
                    {
                        serviceName = applicationName;
                    }

                    var endpoints = new Uri[] { new Uri(endpoint) };

                    // One plain index per UTC day of each event: {service}-{env}-{yyyy-MM-dd}-generic-default.
                    var transport = ElasticsearchDailyIndexSink.CreateTransport(endpoints, username!, password!);
                    var channelOptions = ElasticsearchDailyIndexSink.CreateChannelOptions(transport, serviceName!, environment);

                    logger.WriteTo.Async(w => w.Sink(new ElasticsearchDailyIndexSink(
                        channelOptions,
                        new EcsTextFormatterConfiguration<LogEventEcsDocument>(),
                        BootstrapMethod.Failure)));
                }
            }

            return logger;
        }

        public static Action<HostBuilderContext, LoggerConfiguration> Configure =>
            (context, configuration) =>
            {
                var applicationName = context.HostingEnvironment.ApplicationName?.ToLowerInvariant().Replace(".", "-") ?? "UnknownApp";
                var environment = context.HostingEnvironment.EnvironmentName ?? "Development";

                configuration
                    .BaseConfig()
                    .WriteToFile(context.Configuration, applicationName, environment)
                    .WriteToElasticsearch(context.Configuration, applicationName, environment)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithProperty("Environment", environment)
                    .Enrich.WithProperty("Application", applicationName)
                    .ReadFrom.Configuration(context.Configuration);
            };
    }
}