using Azure.Identity;
using Light.Graph;
using Light.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Graph;
using System;
using System.Collections.Generic;

namespace Light.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a <see cref="GraphServiceClient"/> (singleton, app-only client-credentials auth),
        /// <see cref="IGraphMailService"/> (scoped) and <see cref="IGraphTeams"/> (transient).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="GraphOptions.TenantId"/>, <see cref="GraphOptions.ClientId"/> and
        /// <see cref="GraphOptions.ClientSecret"/> are validated immediately; an
        /// <see cref="ArgumentException"/> is thrown if any is missing.
        /// </para>
        /// <para>
        /// The <see cref="GraphServiceClient"/> is registered with <c>TryAddSingleton</c>, so a client the
        /// consumer registered <b>before</b> calling this method is kept. The scoped/transient service
        /// lifetimes are stateless wrappers around that shared client.
        /// </para>
        /// </remarks>
        public static IServiceCollection AddMicrosoftGraph(this IServiceCollection services, Action<GraphOptions> action)
        {
            // bind action to options
            var options = new GraphOptions();
            action.Invoke(options);

            ValidateOptions(options);

            var credential = new ClientSecretCredential(
                options.TenantId, options.ClientId, options.ClientSecret,
                new TokenCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                });

            //you can use a single client instance for the lifetime of the application
            services.TryAddSingleton(sp =>
            {
                return new GraphServiceClient(credential);
            });

            var allowedSenders = options.AllowedSenders is null
                ? null
                : new List<string>(options.AllowedSenders);

            services.AddScoped<IGraphMailService>(sp =>
                new GraphMailService(sp.GetRequiredService<GraphServiceClient>(), allowedSenders));

            services.AddTransient<IGraphTeams, GraphTeamsService>();

            return services;
        }

        private static void ValidateOptions(GraphOptions options)
        {
            var missing = new List<string>();

            if (string.IsNullOrWhiteSpace(options.TenantId)) missing.Add(nameof(GraphOptions.TenantId));
            if (string.IsNullOrWhiteSpace(options.ClientId)) missing.Add(nameof(GraphOptions.ClientId));
            if (string.IsNullOrWhiteSpace(options.ClientSecret)) missing.Add(nameof(GraphOptions.ClientSecret));

            if (missing.Count > 0)
            {
                throw new ArgumentException(
                    $"Microsoft Graph is not configured: {string.Join(", ", missing)} must be set on {nameof(GraphOptions)} " +
                    $"when calling {nameof(AddMicrosoftGraph)}.",
                    "action");
            }
        }
    }
}
