namespace UnitTests.ExtensionsTests
{
    public class EnumTests
    {
        [Fact]
        public void Should_Correct_Values()
        {
            var enumDesciption1 = TestEnum.Value1.GetDescription();
            enumDesciption1.ShouldBe("Description 1");

            var enumNameOfDisplay1 = TestEnum.Value1.GetNameOfDisplay();
            enumNameOfDisplay1.ShouldBe("Name of Display 1");

            var enumDescriptionOfDisplay1 = TestEnum.Value1.GetDescriptionOfDisplay();
            enumDescriptionOfDisplay1.ShouldBe("Description of Display 1");

            var enumNameOfDisplay2 = TestEnum.Value2.GetNameOfDisplay();
            enumNameOfDisplay2.ShouldBe(null);

            var enumDescriptionOfDisplay2 = TestEnum.Value2.GetDescriptionOfDisplay();
            enumDescriptionOfDisplay2.ShouldBe(null);
        }

        [Fact]
        public void Should_Correct_Options()
        {
            var enumOptions = EnumHelper.GetOptions<TestEnum>();
            foreach (var option in enumOptions)
            {
                option.StringValue.ShouldBe($"Value{option.Value}");

                option.Description.ShouldBe($"Description {option.Value}");
            }
        }
    }
}