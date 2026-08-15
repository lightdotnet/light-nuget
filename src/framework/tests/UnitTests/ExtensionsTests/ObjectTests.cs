namespace UnitTests.ExtensionsTests
{
    public class ObjectTests
    {
        [Test]
        public void Should_Check_ExactlyType()
        {
            var list = new List<object>();
            var dictionary = new Dictionary<string, object>();

            list.IsListOfT().ShouldBeTrue();
            dictionary.IsListOfT().ShouldBeFalse();

            list.IsDictionary().ShouldBeFalse();
            dictionary.IsDictionary().ShouldBeTrue();
        }
    }
}