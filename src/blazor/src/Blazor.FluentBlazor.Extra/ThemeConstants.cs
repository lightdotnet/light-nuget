using Microsoft.FluentUI.AspNetCore.Components;
using Icons20 = Microsoft.FluentUI.AspNetCore.Components.Icons.Regular.Size20;

namespace Light.FluentBlazor;

public class ViewIcon
{
    public static Icon Search => new Icons20.Search();

    public static Icon View => new Icons20.ContentView();

    public static Icon Refresh => new Icons20.ArrowClockwise();

    public static Icon Add => new Icons20.AddCircle();

    public static Icon Edit => new Icons20.Edit();

    public static Icon Delete => new Icons20.Delete();

    public static Icon Cancel => new Icons20.Dismiss();
}