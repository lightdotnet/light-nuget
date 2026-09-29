namespace UnitTests.ExtensionsTests
{
    public class EnumTests
    {
        [Test]
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

        [Test]
        public void Should_Correct_Options()
        {
            var enumOptions = EnumHelper.GetOptions<TestEnum>();
            foreach (var option in enumOptions)
            {
                option.StringValue.ShouldBe($"Value{option.Value}");

                option.Description.ShouldBe($"Description {option.Value}");
            }
        }

        private enum LongEnum : long
        {
            [System.ComponentModel.Description("Big")]
            Big = 5_000_000_000,
            Small = 1,
        }

        private enum ByteEnum : byte
        {
            One = 1,
        }

        [Flags]
        private enum FlagsEnum
        {
            [System.ComponentModel.Description("A")]
            A = 1,
            B = 2,
        }

        [Test]
        public void GetOptions_Should_Support_Non_Int_Underlying_Types()
        {
            var longOptions = EnumHelper.GetOptions<LongEnum>().ToList();
            var big = longOptions.Single(x => x.StringValue == nameof(LongEnum.Big));
            big.LongValue.ShouldBe(5_000_000_000L);
            big.Description.ShouldBe("Big");
            longOptions.Single(x => x.StringValue == nameof(LongEnum.Small)).Value.ShouldBe(1);

            EnumHelper.GetOptions<ByteEnum>().Single().Value.ShouldBe(1);
        }

        [Test]
        public void Attribute_Getters_Should_Return_Null_For_Undefined_Or_Combined_Values()
        {
            (FlagsEnum.A | FlagsEnum.B).GetDescription().ShouldBe(null);
            ((FlagsEnum)64).GetNameOfDisplay().ShouldBe(null);
            ((TestEnum)99).GetDescriptionOfDisplay().ShouldBe(null);
        }
    }
}
