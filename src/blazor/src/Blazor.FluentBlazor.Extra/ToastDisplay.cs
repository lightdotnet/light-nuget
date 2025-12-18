using Light.Blazor;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Light.FluentBlazor;

internal class ToastDisplay(IToastService service) : IToastDisplay
{
    public void ShowSuccess(string message) => service.ShowSuccess(message);

    public void ShowWarning(string message) => service.ShowWarning(message);

    public void ShowError(string message) => service.ShowError(message);

    public void Clear() => service.ClearAll();
}