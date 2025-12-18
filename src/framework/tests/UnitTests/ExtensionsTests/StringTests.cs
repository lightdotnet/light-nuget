namespace UnitTests.ExtensionsTests
{
    public class StringTests
    {
        private const string _test = "A1B2C_3D4E5_6F7G8";
        private const string _by = "_";

        [Fact]
        public void Should_Get_Characters_Correct()
        {
            var left4Chars = _test.Left(4);
            left4Chars.ShouldBe("A1B2");

            var right4Chars = _test.Right(4);
            right4Chars.ShouldBe("F7G8");
        }

        [Fact]
        public void Should_Get_Characters_By_Correct()
        {
            var leftFrom = _test.Left(_by);
            leftFrom.ShouldBe("A1B2C");

            var rightFrom = _test.Right(_by);
            rightFrom.ShouldBe("6F7G8");
        }
    }
}