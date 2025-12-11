using Light.Blazor;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Light.FluentBlazor;

internal class DialogDisplay(IDialogService dialogService) : IDialogDisplay
{
    public async Task<bool> ShowConfirm(string message)
    {
        var dialog = await dialogService.ShowWarningAsync(message);

        DialogResult? dialogResult = await dialog.Result;

        return !dialogResult.Cancelled;
    }
}
