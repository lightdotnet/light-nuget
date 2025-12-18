using Light.Blazor;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Light.MudBlazor;

public static class DependencyInjection
{
    public static IServiceCollection AddMudBlazorExtraComponents(this IServiceCollection services)
    {
        services.AddMudServices();

        services.AddScoped<IDialogDisplay, DialogDisplay>();
        services.AddScoped<IToastDisplay, ToastDisplay>();
        services.AddScoped<SpinnerService>();

        services.AddScoped<ICallGuardedService, CallGuardedService>();

        return services;
    }
}
