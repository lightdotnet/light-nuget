using Light.Blazor;
using Light.Contracts;
using MudBlazor;

namespace Light.MudBlazor;

public interface ICallGuardedService : ICallGuarded
{
    Task<Result> ExecuteAsync(Func<Task<Result>> call, string successMessage, IMudDialogInstance dialog);

    Task<Result> GetDialogResultAsync(IDialogReference dialog);
}

public class CallGuardedService(
    IToastDisplay toast,
    IDialogDisplay dialog,
    SpinnerService spinner) : CallGuarded(toast, dialog, spinner), ICallGuardedService
{
    public async Task<Result> ExecuteAsync(Func<Task<Result>> call, string successMessage, IMudDialogInstance dialog)
    {
        var result = await ExecuteAsync(call, successMessage);

        if (result.Succeeded)
        {
            dialog.Close(DialogResult.Ok(result));
        }

        return result;
    }

    public async Task<Result> GetDialogResultAsync(IDialogReference dialog)
    {
        var dialogResult = await dialog.Result;

        if (dialogResult != null && dialogResult.Data != null)
        {
            var result = (Result)dialogResult.Data;

            return result;
        }

        return Result.Error("Error when get dialog result");
    }
}
