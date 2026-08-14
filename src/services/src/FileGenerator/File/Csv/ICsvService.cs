using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Light.File.Csv
{
    public interface ICsvService
    {
        string[]? ReadHeaders(StreamReader streamReader);

        string[]? ReadHeaders(Stream stream);

        IEnumerable<T> ReadAs<T>(StreamReader streamReader);

        IEnumerable<T> ReadAs<T>(Stream stream);

        CsvData<T>? Read<T>(StreamReader streamReader);

        CsvData<T>? Read<T>(Stream stream);

        DictionaryData? Read(StreamReader streamReader);

        DictionaryData? Read(Stream stream);

        Task<Stream> WriteAsync<T>(IEnumerable<T> records, bool excludeHeader = false);

        Task<Stream> WriteAsync(DataTable table, bool excludeHeader = false);
    }
}
