namespace Light.Blazor;

public class DataTable<T>
{
    private readonly Func<Task<Result<IEnumerable<T>>>>? _resultFunc;

    private readonly Func<Task<IEnumerable<T>>>? _queryFunc;

    private IEnumerable<T> _records = [];

    private Func<T, bool>? _searchFunc;

    public DataTable(Func<Task<Result<IEnumerable<T>>>> func)
    {
        _resultFunc = func;
    }

    public DataTable(Func<Task<IEnumerable<T>>> func)
    {
        _queryFunc = func;
    }

    public DataTable(IEnumerable<T> data)
    {
        _records = data;
    }

    public Func<string, T, bool>? SearchFunc { get; init; }

    public IEnumerable<T> Records { get; private set; } = [];

    public Paged Pagination { get; private set; } = new();

    public int PageSize { get; set; } = 20;

    public bool Processing { get; private set; } = false;

    public string? ErrorMessage { get; private set; }

    public async Task ReloadAsync()
    {
        Processing = true;

        try
        {
            if (_resultFunc is not null)
            {
                var getData = await _resultFunc();

                if (getData.Succeeded)
                {
                    _records = getData.Data;
                    ErrorMessage = null;
                }
                else
                {
                    ClearData();
                    ErrorMessage = getData.Message;
                }
            }
            else if (_queryFunc is not null)
            {
                _records = await _queryFunc();
            }

            DataPagination();
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
        Pagination = new();
        Records = [];
    }

    public Task RunUpdateAsync(IPage? update)
    {
        if (update != null)
        {
            Pagination.Page = update.Page;
            Pagination.PageSize = update.PageSize;
        }

        DataPagination();

        return Task.CompletedTask;
    }

    public Task SearchAsync(string? value = null)
    {
        Pagination.Page = 1;

        if (string.IsNullOrEmpty(value) || SearchFunc is null)
        {
            _searchFunc = null;
        }
        else
        {
            _searchFunc = e => SearchFunc(value, e);
        }

        DataPagination();

        return Task.CompletedTask;
    }

    private void DataPagination()
    {
        var data = _records;

        if (_searchFunc != null)
        {
            data = data.Where(_searchFunc);
        }

        var pagedData = data.ToPaged(Pagination.Page, PageSize);

        Pagination = pagedData;
        Records = pagedData.Records;
    }
}
