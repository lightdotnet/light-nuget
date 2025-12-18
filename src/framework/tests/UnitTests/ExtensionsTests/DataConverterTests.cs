namespace UnitTests.ExtensionsTests
{
    public class DataConverterTests
    {
        private readonly string _intValue = "1|2|3|4|5";
        private readonly string[] _intArray = ["1", "2", "3", "4", "5"];

        private readonly string _strValue = "A|B|C|D|E";
        private readonly string[] _strArray = ["A", "B", "C", "D", "E"];

        [Fact]
        public void Should_Return_Array()
        {
            var intArray = _intValue.ToArray();
            LightAssert.ShouldBe(intArray, _intArray);

            var strArray = _strValue.ToArray();
            LightAssert.ShouldBe(strArray, _strArray);
        }

        [Fact]
        public void Should_Return_String()
        {
            var intValue = _intArray.JoinToString();
            LightAssert.ShouldBe(intValue, _intValue);

            var strValue = _strArray.JoinToString();
            LightAssert.ShouldBe(strValue, _strValue);
        }

        [Fact]
        public void Should_Return_Correct_Values()
        {
            var model = new TestObject
            {
                Id = 1,
                Value = "Name Of Id 1",
            };

            var values = model.GetValues();

            values["Id"].ShouldBe(1);
            values["Value"].ShouldBe("Name Of Id 1");
        }


        [Fact]
        public void Check_Object_Is_A_List()
        {
            var single = new TestObject();
            single.IsList().ShouldBe(false);

            var list = new List<TestObject>();
            list.IsList().ShouldBe(true);
        }
    }
}