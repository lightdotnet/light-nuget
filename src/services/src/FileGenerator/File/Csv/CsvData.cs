using System.Collections.Generic;

namespace Light.File.Csv
{
    public class CsvData<T>
    {
        public string[] Headers { get; set; } = null!;

        public IEnumerable<T> Rows { get; set; } = new List<T>();
    }
}
