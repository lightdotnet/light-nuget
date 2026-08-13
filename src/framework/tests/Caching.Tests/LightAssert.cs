using NUnit.Framework;

namespace UnitTests
{
    internal static class LightAssert
    {
        internal static void ShouldBe<T>(this T value, T equalTo) =>
            NUnit.Framework.Assert.That(value, Is.EqualTo(equalTo));

        internal static void ShouldNotBeNullOrEmpty<T>(this T value) =>
            NUnit.Framework.Assert.That(value, Is.Not.Null.And.Not.Empty);

        internal static void ShouldContains<T>(this IEnumerable<T> value, object obj) =>
             NUnit.Framework.Assert.That(value, Contains.Item(obj));

        internal static void ShouldBeTrue(this bool value) => ShouldBe(value, true);

        internal static void ShouldBeFalse(this bool value) => ShouldBe(value, false);
    }
}
