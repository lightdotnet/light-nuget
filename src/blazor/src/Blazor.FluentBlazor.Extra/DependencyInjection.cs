using Light.Blazor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Light.FluentBlazor;

public static class DependencyInjection
{
    public static IServiceCollection AddFluentBlazorExtraComponents(this IServiceCollection services)
    {
        services.AddFluentUIComponents();

        services.AddScoped<IDialogDisplay, DialogDisplay>();
        services.AddScoped<IToastDisplay, ToastDisplay>();
        services.AddScoped<SpinnerService>();

        services.AddScoped<ICallGuardedService, CallGuardedService>();

        return services;
    }
}
