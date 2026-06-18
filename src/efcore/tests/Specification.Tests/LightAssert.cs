namespace Specification.Tests;

public static class LightAssert
{
    public static void ShouldBe<T>(this T value, T equalTo) =>
        Assert.That(value, Is.EqualTo(equalTo));

    public static void ShouldNotBeNullOrEmpty<T>(this T value) =>
        Assert.That(value, Is.Not.Null.And.Not.Empty);

    public static void ShouldContains<T>(this IEnumerable<T> value, object obj) =>
         Assert.That(value, Contains.Item(obj));

    public static void ShouldBeTrue(this bool value) => value.ShouldBe(true);

    public static void ShouldBeFalse(this bool value) => value.ShouldBe(false);

    public static void ShouldBeNull<T>(this T? value) =>
        Assert.That(value, Is.Null);

    public static void ShouldNotBeNull<T>(this T? value) =>
        Assert.That(value, Is.Not.Null);

    public static void ShouldBeGreaterThan(this int value, int compareTo) =>
        Assert.That(value, Is.GreaterThan(compareTo));

    public static void ShouldHaveCount<T>(this IEnumerable<T> value, int expectedCount) =>
        Assert.That(value.Count(), Is.EqualTo(expectedCount));

    public static void ShouldBeInstanceOf<T>(this object value) =>
        Assert.That(value, Is.InstanceOf<T>());
}