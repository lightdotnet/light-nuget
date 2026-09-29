namespace UnitTests.ExtensionsTests;

public class UriBuilderTests
{
    [Test]
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

    [Test]
    public void Should_Handle_Null_Values_And_Encode_Names()
    {
        var query = new Dictionary<string, object>
        {
            { "a b", null! },
            { "price", 1.5m },
        };

        var uriQuery = UriQueryBuilder.ToQueryString(query);

        uriQuery.ShouldBe("a+b=&price=1.5");
    }
}
