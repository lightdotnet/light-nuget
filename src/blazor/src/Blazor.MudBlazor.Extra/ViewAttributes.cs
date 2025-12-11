using MudBlazor;

namespace Light.MudBlazor;

public enum AppColor
{
    Default,
    Primary,
    Secondary,
    Warning,
    Danger,
    Info,
}

public enum AppSize
{
    Small,
    Default,
    Large,
}

public static class MappingExtensions
{
    public static Color Map(this AppColor color) => color switch
    {
        AppColor.Default => Color.Default,
        AppColor.Primary => Color.Primary,
        AppColor.Secondary => Color.Secondary,
        AppColor.Warning => Color.Warning,
        AppColor.Danger => Color.Error,
        AppColor.Info => Color.Info,
        _ => Color.Transparent,
    };

    public static Size Map(this AppSize color) => color switch
    {
        AppSize.Default => Size.Medium,
        AppSize.Small => Size.Small,
        AppSize.Large => Size.Large,
        _ => Size.Medium,
    };
}