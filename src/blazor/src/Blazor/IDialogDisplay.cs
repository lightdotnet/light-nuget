namespace Light.Blazor;

public interface IDialogDisplay
{
    Task<bool> ShowConfirm(string confirmMessage);
}
