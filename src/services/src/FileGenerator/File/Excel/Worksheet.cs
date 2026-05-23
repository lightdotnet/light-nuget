namespace Light.File.Excel
{
    public class Worksheet
    {
        public Worksheet(object data, string? sheetName = null)
        {
            Data = data;
            SheetName = sheetName;
        }

        public object Data { get; set; }

        public string? SheetName { get; set; }
    }
}
