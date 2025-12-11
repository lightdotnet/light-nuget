using Light.Blazor;
using MudBlazor;

namespace Light.MudBlazor;

internal class ToastDisplay(ISnackbar snackbar) : IToastDisplay
{
    public void ShowSuccess(string message) => snackbar.Add(message, Severity.Success);

    public void ShowWarning(string message) => snackbar.Add(message, Severity.Warning);

    public void ShowError(string message) => snackbar.Add(message, Severity.Error);

    public void Clear() => snackbar.Clear();
}