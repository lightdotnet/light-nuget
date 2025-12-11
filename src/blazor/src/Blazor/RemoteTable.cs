namespace Light.Blazor;

public class RemoteTable<TResult, TLookup>(Func<TLookup, Task<PagedResult<TResult>>> queryFunc)
    where TResult : class
    where TLookup : class, IPage
{
    private readonly Func<TLookup, Task<PagedResult<TResult>>> _queryFunc = queryFunc;

    public ServerExport? ExportApi;

    public TLookup Lookup { get; set; } = Activator.CreateInstance<TLookup>();

    public IEnumerable<TResult> Records { get; private set; } = [];

    public Paged? Pagination { get; private set; }

    public bool Processing { get; private set; } = false;

    public string? ErrorMessage { get; private set; }

    public async Task ReloadAsync()
    {
        Processing = true;

        try
        {
            var getData = await _queryFunc(Lookup);

            if (getData.Succeeded)
            {
                Pagination = getData.Data;
                Records = getData.Data.Records;
                ErrorMessage = null;
            }
            else
            {
                ClearData();
                ErrorMessage = getData.Message;
            }
        }
        catch (Exception ex)
        {
            ClearData();
            ErrorMessage = ex.Message;
        }

        Processing = false;
    }

    private void ClearData()
    {
        Pagination = null;
        Records = [];
    }

    public Task RunUpdateAsync(IPage? update)
    {
        if (update != null)
        {
            Lookup.Page = update.Page;
            Lookup.PageSize = update.PageSize;
        }

        return ReloadAsync();
    }

    public Task SearchAsync()
    {
        Lookup.Page = 1;

        return ReloadAsync();
    }

    /// <summary>
    /// Export data from server
    /// </summary>
    /// <param name="export"></param>
    /// <param name="fileName"></param>

    public class ServerExport(string fileName)
    {
        public ServerExport(Func<Task<Stream>> exportFunc, string fileName) : this(fileName)
        {
            ExportFunc = exportFunc;
        }

        public ServerExport(Func<TLookup, Task<Stream>> exportFunc, string fileName) : this(fileName)
        {
            ExportByLookupFunc = exportFunc;
        }

        public string FileName { get; set; } = fileName;

        public Func<Task<Stream>>? ExportFunc { get; init; }

        public Func<TLookup, Task<Stream>>? ExportByLookupFunc { get; init; }

        public string ButtonName { get; set; } = "Export";
    }
}