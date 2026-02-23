namespace Light.Blazor;

public interface IDialogDisplay
{
    Task<bool> ShowConfirmAsync(string confirmMessage);
}
