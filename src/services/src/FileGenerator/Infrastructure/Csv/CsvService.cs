using CsvHelper;
using CsvHelper.Configuration;
using Light.File.Csv;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Infrastructure.Csv
{
    public class CsvService : ICsvService
    {
        private readonly CsvConfiguration _config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            HeaderValidated = null,   // Skips missing headers during mapping
            MissingFieldFound = null, // Skips missing *fields* in rows
        };

        // ─── Read ────────────────────────────────────────────────────────────────

        public string[]? ReadHeaders(StreamReader streamReader)
        {
            using var csv = new CsvReader(streamReader, _config);
            csv.Read();
            csv.ReadHeader();
            return csv.HeaderRecord;
        }

        public string[]? ReadHeaders(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return ReadHeaders(reader);
        }

        public IEnumerable<T> ReadAs<T>(StreamReader streamReader)
        {
            using var csv = new CsvReader(streamReader, _config);
            csv.Context.TypeConverterCache.AddConverter<object>(new ObjectConverter());
            return csv.GetRecords<T>().ToList(); // ToList() để enumerate trước khi dispose
        }

        public IEnumerable<T> ReadAs<T>(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return ReadAs<T>(reader);
        }

        public CsvData<T>? Read<T>(StreamReader streamReader)
        {
            using var csv = new CsvReader(streamReader, _config);
            csv.Context.TypeConverterCache.AddConverter<object>(new ObjectConverter());

            var records = csv.GetRecords<T>().ToList();
            var headers = csv.HeaderRecord;

            if (headers is null) return null;

            return new CsvData<T>
            {
                Headers = headers,
                Rows = records
            };
        }

        public CsvData<T>? Read<T>(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return Read<T>(reader);
        }

        public DictionaryData? Read(StreamReader streamReader)
        {
            using var csv = new CsvReader(streamReader, _config);
            csv.Context.TypeConverterCache.AddConverter<object>(new ObjectConverter());

            // Read the first row to get the headers
            csv.Read();
            csv.ReadHeader();

            var headers = csv.HeaderRecord;
            if (headers is null) return null;

            var rows = new List<IDictionary<string, object?>>();

            while (csv.Read())
            {
                var row = new Dictionary<string, object?>();
                foreach (var header in headers)
                    row[header] = csv.GetField(header);
                rows.Add(row);
            }

            return new DictionaryData
            {
                Headers = headers,
                Rows = rows
            };
        }

        public DictionaryData? Read(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return Read(reader);
        }

        // ─── Write ───────────────────────────────────────────────────────────────

        public async Task<Stream> WriteAsync<T>(IEnumerable<T> records, bool excludeHeader = false)
        {
            var memoryStream = new MemoryStream();

            await using var writer = new StreamWriter(memoryStream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            if (!excludeHeader)
            {
                csv.WriteHeader<T>();
                await csv.NextRecordAsync();
            }

            foreach (var record in records)
            {
                csv.WriteRecord(record);
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync(); // Ensure all data is written to the stream

            memoryStream.Seek(0, SeekOrigin.Begin); // Reset stream position for reading

            return memoryStream;
        }

        public async Task<Stream> WriteAsync(DataTable table, bool excludeHeader = false)
        {
            var memoryStream = new MemoryStream();

            await using var writer = new StreamWriter(memoryStream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            if (!excludeHeader)
            {
                foreach (DataColumn column in table.Columns)
                    csv.WriteField(column.ColumnName);

                await csv.NextRecordAsync(); // moves to a new line
            }

            foreach (DataRow row in table.Rows)
            {
                foreach (var item in row.ItemArray)
                    csv.WriteField(item);

                await csv.NextRecordAsync(); // without this, all data stays in the same row
            }

            await writer.FlushAsync(); // Ensure all data is written to the stream

            memoryStream.Seek(0, SeekOrigin.Begin); // Reset stream position for reading

            return memoryStream;
        }
    }
}
