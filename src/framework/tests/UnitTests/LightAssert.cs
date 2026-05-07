using NUnit.Framework;

namespace UnitTests
{
    internal static class LightAssert
    {
        internal static void ShouldBe<T>(this T value, T equalTo) =>
            Assert.That(value, Is.EqualTo(equalTo));

        internal static void ShouldNotBeNullOrEmpty<T>(this IEnumerable<T> value)
        {
            Assert.NotNull(value);
            Assert.IsNotEmpty(value);
        }

        internal static void ShouldBeTrue(this bool value) =>
            Assert.True(value);

        internal static void ShouldBeFalse(this bool value) =>
            Assert.False(value);
    }
}