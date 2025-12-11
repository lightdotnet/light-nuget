namespace Specification.Tests
{
    internal static class LightAssert
    {
        internal static void ShouldBe<T>(this T value, T equalTo) =>
            Assert.That(value, Is.EqualTo(equalTo));

        internal static void ShouldNotBeNullOrEmpty<T>(this T value) =>
            Assert.That(value, Is.Not.Null.And.Not.Empty);

        internal static void ShouldContains<T>(this IEnumerable<T> value, object obj) =>
             Assert.That(value, Contains.Item(obj));

        internal static void ShouldBeTrue(this bool value) => value.ShouldBe(true);

        internal static void ShouldBeFalse(this bool value) => value.ShouldBe(false);
    }
}
