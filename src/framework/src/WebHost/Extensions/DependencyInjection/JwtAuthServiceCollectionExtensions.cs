using Light.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Light.Extensions.DependencyInjection;

public static class JwtAuthServiceCollectionExtensions
{
    /// <summary>
    /// Add JWT Authentication with validate Issuer & SecretKey
    /// </summary>
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, string issuer, string secretKey, JwtBearerEvents jwtBearerEvents, string roleClaimType)
    {
        var keyAsBytes = Encoding.ASCII.GetBytes(secretKey);

        services
            .AddAuthentication(authentication =>
            {
                authentication.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authentication.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(bearer =>
            {
                bearer.RequireHttpsMetadata = false;
                bearer.SaveToken = true;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyAsBytes),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = false,
                    RoleClaimType = roleClaimType,
                    ClockSkew = TimeSpan.Zero
                };

                bearer.Events = jwtBearerEvents;
            });

        return services;
    }

    public static IServiceCollection AddJwtAuth(this IServiceCollection services, string issuer, string secretKey, string roleClaimType, string signalRHub = "/signalr-hub")
    {
        var jwtBearerEvents = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                if (!context.Response.HasStarted)
                {
                    throw new UnauthorizedException("Authentication Failed.");
                }

                return Task.CompletedTask;
            },
            OnForbidden = _ => throw new ForbiddenException("You are not authorized to access this resource."),
            OnMessageReceived = context =>
            {
                if (context.HttpContext.Request.Path.StartsWithSegments(signalRHub))
                {
                    var accessToken = context.Request.Query["access_token"];

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        // Read the token out of the query string
                        context.Token = accessToken;
                    }
                }

                return Task.CompletedTask;
            }
        };

        return services.AddJwtAuth(issuer, secretKey, jwtBearerEvents, roleClaimType);
    }
}