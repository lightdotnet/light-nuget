namespace UnitTests.ExtensionsTests
{
    public class GetAttributeTests
    {
        [Fact]
        public void Should_Return_Correct_Attribute()
        {
            var type = typeof(TestObject);

            var displayName = type.GetDisplayName();
            displayName.ShouldBe("Test DisplayName");

            var description = type.GetDescription();
            description.ShouldBe("Test Description");

            var nameInDisplay = type.GetNameOfDisplay();
            nameInDisplay.ShouldBe("Test Name in Display");

            var descInDisplay = type.GetDescriptionOfDisplay();
            descInDisplay.ShouldBe("Test Description in Display");

            var nameOfId = ObjectHelper.GetPropertyName<TestObject>(x => x.Id);
            nameOfId.ShouldBe("Id");

            var nameOfValue = ObjectHelper.GetPropertyName<TestObject>(x => x.Value);
            nameOfValue.ShouldBe("Value");
        }
    }
}