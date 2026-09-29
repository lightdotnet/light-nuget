using Light.EventBus.Events;
using Light.MassTransit.RabbitMQ;
using MassTransit;

namespace EventBus.Tests;

public class BindingNameTests
{
    private sealed class OriginalFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => "original:" + typeof(T).Name;
    }

    private readonly BusEntityBindingNameFormatter _formatter = new(new OriginalFormatter());

    [Test]
    public void FormatEntityName_UsesBindingName_WhenDeclared()
    {
        Assert.That(_formatter.FormatEntityName<BaseBoundEvent>(), Is.EqualTo("base-event"));
    }

    [Test]
    public void FormatEntityName_FallsBackToOriginal_WhenNoAttribute()
    {
        Assert.That(_formatter.FormatEntityName<UnboundEvent>(), Is.EqualTo("original:UnboundEvent"));
    }

    [Test]
    public void FormatEntityName_DoesNotInheritBindingName()
    {
        Assert.That(_formatter.FormatEntityName<DerivedFromBoundEvent>(), Is.EqualTo("original:DerivedFromBoundEvent"));
    }

    [Test]
    public void BindingNameAttribute_IsNotInherited()
    {
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(typeof(BindingNameAttribute), typeof(AttributeUsageAttribute))!;

        Assert.That(usage.Inherited, Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void BindingNameAttribute_RejectsBlankName(string? name)
    {
        var ex = Assert.Throws<ArgumentException>(() => new BindingNameAttribute(name!));

        Assert.That(ex!.ParamName, Is.EqualTo("bindingName"));
    }

    [Test]
    public void ConsumerDefinition_UsesBindingNameAsEndpointName_ByDefault()
    {
        IConsumerDefinition definition = new DefaultBoundConsumerDefinition();

        Assert.That(definition.GetEndpointName(KebabCaseEndpointNameFormatter.Instance), Is.EqualTo("base-event"));
    }

    [Test]
    public void ConsumerDefinition_AppliesEndpointNamePrefix()
    {
        IConsumerDefinition definition = new PrefixedBoundConsumerDefinition();

        Assert.That(definition.GetEndpointName(KebabCaseEndpointNameFormatter.Instance), Is.EqualTo("billing-base-event"));
    }

    [Test]
    public void ConsumerDefinition_RejectsInvalidEndpointNamePrefix()
    {
        Assert.Throws<ArgumentException>(() => _ = new InvalidPrefixBoundConsumerDefinition());
    }
}
