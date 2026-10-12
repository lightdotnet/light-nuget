namespace UnitTests.ExtensionsTests
{
    public class ValueHandlerTests
    {
        private class Sample
        {
            private readonly Dictionary<int, string> _items = new() { { 0, "item" } };

            public string? Name { get; set; }

            public string ReadOnlyName => "read-only value";

            public DateTime Date { get; set; }

            public DateTime? NullableDate { get; set; }

            public string this[int index]
            {
                get => _items[index];
                set => _items[index] = value;
            }
        }

        [Test]
        public void Should_Return_Null_When_Input_Is_Null()
        {
            Sample? data = null;

            data.MaximumCharHandler(3).ShouldBe(null);
            data.NullStringHandler().ShouldBe(null);
            data.NullDateTimeHandler().ShouldBe(null);
        }

        [Test]
        public void MaximumCharHandler_Should_Skip_ReadOnly_And_Indexer_Properties()
        {
            var data = new Sample { Name = "abcdef" }.MaximumCharHandler(3);

            data.Name.ShouldBe("abc");
            data.ReadOnlyName.ShouldBe("read-only value");
            data[0].ShouldBe("item");
        }

        [Test]
        public void NullStringHandler_Should_Set_Empty_String()
        {
            var data = new Sample().NullStringHandler();

            data.Name.ShouldBe(string.Empty);
        }

        [Test]
        public void NullDateTimeHandler_Should_Set_Default_For_Min_And_Null_Dates()
        {
            var defaultTime = new DateTime(2000, 1, 1);

            var data = new Sample().NullDateTimeHandler(defaultTime);

            data.Date.ShouldBe(defaultTime);
            data.NullableDate.ShouldBe(defaultTime);
        }
    }
}
