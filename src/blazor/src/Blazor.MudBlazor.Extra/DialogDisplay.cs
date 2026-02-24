using Light.Blazor;
using MudBlazor;

namespace Light.MudBlazor;

internal class DialogDisplay(IDialogService dialogService) : IDialogDisplay
{
    public async Task<bool> ShowConfirmAsync(string message) =>
        await dialogService.ShowMessageBoxAsync("Warning", message, options: new DialogOptions
        {
            CloseButton = true,
        }) ?? false;
}
