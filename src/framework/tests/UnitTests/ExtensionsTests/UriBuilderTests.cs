namespace UnitTests.ExtensionsTests;

public class UriBuilderTests
{
    [Fact]
    public void Should_Build_Correct_Values()
    {
        var query = new Dictionary<string, object>
        {
            { "id", 1 },
            { "name", "Hello" }
        };

        var uriQuery = UriQueryBuilder.ToQueryString(query);

        uriQuery.ShouldBe("id=1&name=Hello");
    }
}
