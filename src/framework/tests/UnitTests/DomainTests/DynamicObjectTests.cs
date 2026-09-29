using System.Globalization;
using Light.Extensions.DynamicObject;

namespace UnitTests.DomainTests
{
    public class DynamicObjectTests
    {
        private class TestDynamicEntity : DynamicEntity
        {
        }

        private enum Status
        {
            Active = 1,
        }

        private class Sample
        {
            private readonly string[] _items = { "item" };

            public string? Name { get; set; }

            public int? Count { get; set; }

            public bool? IsEnabled { get; set; }

            public decimal? Amount { get; set; }

            public double Ratio { get; set; }

            public DateTime Date { get; set; }

            public DateTimeOffset? DateOffset { get; set; }

            public Status State { get; set; }

            public int? Missing { get; set; }

            public string this[int index] => _items[index];
        }

        [Test]
        public void Exporter_Should_Skip_Indexers()
        {
            var columns = DynamicColumnExporter.ConvertToDynamicColumns<Sample, TestDynamicEntity>(new Sample(), "sample");

            columns.Any(c => c.PropName == "Item").ShouldBeFalse();
            columns.Single(c => c.PropName == nameof(Sample.Count)).PropType.ShouldBe("int");
        }

        [Test]
        public void Should_Round_Trip_Nullable_And_Culture_Sensitive_Values()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                // Culture with ',' decimal separator and d/M/y dates.
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                var source = new Sample
                {
                    Name = "name",
                    Count = 5,
                    IsEnabled = true,
                    Amount = 12.34m,
                    Ratio = 0.5,
                    Date = new DateTime(2024, 12, 31, 10, 20, 30, DateTimeKind.Utc),
                    DateOffset = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(7)),
                    State = Status.Active,
                };

                var columns = DynamicColumnExporter.ConvertToDynamicColumns<Sample, TestDynamicEntity>(source, "sample");
                var result = DynamicMapper.MapToObject<Sample, TestDynamicEntity>(columns);

                columns.Single(c => c.PropName == nameof(Sample.Amount)).PropValue.ShouldBe("12.34");
                result.Name.ShouldBe(source.Name);
                result.Count.ShouldBe(source.Count);
                result.IsEnabled.ShouldBe(source.IsEnabled);
                result.Amount.ShouldBe(source.Amount);
                result.Ratio.ShouldBe(source.Ratio);
                result.Date.ShouldBe(source.Date);
                result.Date.Kind.ShouldBe(DateTimeKind.Utc);
                result.DateOffset.ShouldBe(source.DateOffset);
                result.DateOffset!.Value.Offset.ShouldBe(TimeSpan.FromHours(7));
                result.State.ShouldBe(source.State);
                result.Missing.ShouldBe(null);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    }
}
