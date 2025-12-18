using Microsoft.JSInterop;

namespace Light.Blazor;

public interface ICallGuarded
{
    Task<Result> ExecuteAsync(Func<Task<Result>> call, string successMessage = "");

    Task<Result> ExecuteAsync(Func<Task<Result>> call, string successMessage, Func<Task<Result>> runIfSuccess);

    Task<Result> ExecuteAsync(Func<Task<Result>> call, string successMessage, Func<Task> runIfSuccess);

    Task ExecuteIfConfirmAsync(Func<Task<Result>> runFunc, string confirmMessage, string succeededMessage, Func<Task> runIfSuccess);

    Task TryDownloadAsync(Task<Stream> call, string fileName, IJSRuntime jsRuntime);
}

public abstract class CallGuarded(
    IToastDisplay toast,
    IDialogDisplay dialog,
    SpinnerService spinner) : ICallGuarded
{
    protected IToastDisplay Toast => toast;

    protected IDialogDisplay Dialog => dialog;

    protected SpinnerService Spinner => spinner;

    public virtual async Task<Result> ExecuteAsync(Func<Task<Result>> call, string successMessage = "")
    {
        spinner.Show();

        Result result;
        //await Task.Delay(10000);
        try
        {
            result = await call();

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(successMessage))
                {
                    toast.ShowSuccess(successMessage);
                }
                else
                {
                    toast.Clear();
                }
            }
            else
            {
                toast.ShowError(result.Message);
            }
        }
        catch (Exception ex)
        {
            toast.ShowError(ex.Message);

            result = Result.Error(ex.Message);
        }

        spinner.Hide();

        return result;
    }

    public virtual async Task<Result> ExecuteAsync(Func<Task<Result>> call, string successMessage, Func<Task<Result>> runIfSuccess)
    {
        var result = await ExecuteAsync(call, successMessage);

        if (result.Succeeded)
        {
            await runIfSuccess();
        }

        return result;
    }

    public virtual async Task<Result> ExecuteAsync(Func<Task<Result>> call, string successMessage, Func<Task> runIfSuccess)
    {
        var result = await ExecuteAsync(call, successMessage);

        if (result.Succeeded)
        {
            await runIfSuccess();
        }

        return result;
    }

    public virtual async Task ExecuteIfConfirmAsync(Func<Task<Result>> runFunc, string confirmMessage, string succeededMessage, Func<Task> runIfSuccess)
    {
        bool? isConfirm = await dialog.ShowConfirm(confirmMessage);

        if (isConfirm == true)
        {
            await ExecuteAsync(runFunc, succeededMessage, runIfSuccess);
        }
    }

    public virtual async Task TryDownloadAsync(Task<Stream> call, string fileName, IJSRuntime jsRuntime)
    {
        spinner.Show();

        try
        {
            var file = await call;

            await jsRuntime.InvokeVoidAsync("downloadBase64String", fileName, file.ToBase64String());
        }
        catch (Exception ex)
        {
            toast.ShowError(ex.Message);
        }

        spinner.Hide();
    }
}
