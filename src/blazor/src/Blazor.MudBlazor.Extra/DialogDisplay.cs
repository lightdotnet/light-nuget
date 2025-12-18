using Light.Blazor;
using MudBlazor;

namespace Light.MudBlazor;

internal class DialogDisplay(IDialogService dialogService) : IDialogDisplay
{
    public async Task<bool> ShowConfirm(string message) =>
        await dialogService.ShowMessageBox("Warning", message, options: new DialogOptions
        {
            CloseButton = true,
        }) ?? false;
}
