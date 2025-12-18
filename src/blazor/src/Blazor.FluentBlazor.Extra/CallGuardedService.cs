using Light.Blazor;
using Light.Contracts;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Light.FluentBlazor;

public interface ICallGuardedService : ICallGuarded
{
    Task ExecuteAsync(Func<Task<Result>> call, string successMessage, FluentDialog fluentDialog);

    Task ExecuteIfConfirmAsync(Func<Task<IResult>> runFunc, string confirmMessage, string succeededMessage, Func<Task> runIfSuccess);

    Task<Result> GetDialogResultAsync(IDialogReference dialog);
}

public class CallGuardedService(
    IToastDisplay toast,
    IDialogDisplay dialog,
    SpinnerService spinner) : CallGuarded(toast, dialog, spinner), ICallGuardedService
{
    public async Task ExecuteAsync(Func<Task<Result>> call, string successMessage, FluentDialog fluentDialog)
    {
        var result = await base.ExecuteAsync(call, successMessage);

        if (result.Succeeded)
        {
            await fluentDialog.CloseAsync(result);
        }
    }

    public async Task ExecuteIfConfirmAsync(Func<Task<IResult>> runFunc, string confirmMessage, string succeededMessage, Func<Task> runIfSuccess)
    {
        bool? isConfirm = await Dialog.ShowConfirm(confirmMessage);

        if (isConfirm == true)
        {
            var result = await runFunc();

            if (result.Succeeded)
            {
                Toast.ShowSuccess(succeededMessage);
                await runIfSuccess();
            }
        }
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
