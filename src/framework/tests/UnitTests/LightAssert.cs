using System.Collections.Generic;
using Xunit;

namespace UnitTests
{
    internal static class LightAssert
    {
        internal static void ShouldBe<T>(this T value, T expected) =>
            Assert.Equal(expected, value);

        internal static void ShouldNotBeNullOrEmpty<T>(this IEnumerable<T> value)
        {
            Assert.NotNull(value);
            Assert.NotEmpty(value);
        }

        internal static void ShouldContains<T>(this IEnumerable<T> value, T expected) =>
            Assert.Contains(expected, value);

        internal static void ShouldBeTrue(this bool value) =>
            Assert.True(value);

        internal static void ShouldBeFalse(this bool value) =>
            Assert.False(value);
    }
}