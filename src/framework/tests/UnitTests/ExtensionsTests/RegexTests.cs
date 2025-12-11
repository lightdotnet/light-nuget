namespace UnitTests.ExtensionsTests
{
    public class RegexTests
    {
        [Fact]
        public void Should_Remove_Emoji()
        {
            var text = "Text";
            var textWithEmoji = $"{text}😀";

            var textRemovedEmoji = textWithEmoji.RemoveEmoji();

            LightAssert.ShouldBe(text, textRemovedEmoji);
        }
    }
}
