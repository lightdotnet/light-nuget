namespace UnitTests.ExtensionsTests
{
    public class ObjectTests
    {
        [Fact]
        public void Should_Check_ExactlyType()
        {
            var list = new List<object>();
            var dictionary = new Dictionary<string, object>();

            list.IsList().ShouldBeTrue();
            dictionary.IsList().ShouldBeFalse();

            list.IsDictionary().ShouldBeFalse();
            dictionary.IsDictionary().ShouldBeTrue();
        }
    }
}