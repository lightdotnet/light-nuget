using Azure.Identity;
using Light.Graph;
using Light.Graph.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using System;

namespace Light.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMicrosoftGraph(this IServiceCollection services, Action<GraphOptions> action)
        {
            // bind action to options
            var options = new GraphOptions();
            action.Invoke(options);

            var credential = new ClientSecretCredential(
                options.TenantId, options.ClientId, options.ClientSecret,
                new TokenCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                });

            //you can use a single client instance for the lifetime of the application
            services.AddSingleton(sp =>
            {
                return new GraphServiceClient(credential);
            });

            services.AddScoped<IGraphMailService, GraphMailService>();

            services.AddTransient<IGraphTeams, GraphTeamsService>();

            return services;
        }
    }
}