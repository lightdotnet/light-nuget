using System.Text;
using Light.Extensions.DependencyInjection;
using Light.Graph;
using Light.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Serialization.Json;
using NUnit.Framework;

namespace UnitTests.GraphTests;

public class RegistrationValidationTests
{
    private const string FakeId = "00000000-0000-0000-0000-000000000000";

    [TestCase(null, FakeId, "secret", "TenantId")]
    [TestCase(FakeId, "", "secret", "ClientId")]
    [TestCase(FakeId, FakeId, "  ", "ClientSecret")]
    public void AddMicrosoftGraph_MissingCredential_ThrowsArgumentException_NamingIt(
        string? tenantId, string? clientId, string? clientSecret, string expectedName)
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<ArgumentException>(() => services.AddMicrosoftGraph(o =>
        {
            o.TenantId = tenantId;
            o.ClientId = clientId;
            o.ClientSecret = clientSecret;
        }));

        Assert.That(ex!.Message, Does.Contain(expectedName));
    }

    [Test]
    public void AddMicrosoftGraph_NothingConfigured_ListsAllMissing()
    {
        var ex = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddMicrosoftGraph(_ => { }));

        Assert.That(ex!.Message, Does.Contain("TenantId").And.Contain("ClientId").And.Contain("ClientSecret"));
    }

    [Test]
    public void AddMicrosoftGraph_KeepsPreviouslyRegisteredGraphServiceClient()
    {
        var services = new ServiceCollection();
        var own = new GraphServiceClient(new Azure.Identity.ClientSecretCredential(FakeId, FakeId, "own-secret"));
        services.AddSingleton(own);

        services.AddMicrosoftGraph(o =>
        {
            o.TenantId = FakeId;
            o.ClientId = FakeId;
            o.ClientSecret = "fake-secret";
        });

        using var provider = services.BuildServiceProvider();

        Assert.That(ReferenceEquals(provider.GetRequiredService<GraphServiceClient>(), own), Is.True);
        services.Count(d => d.ServiceType == typeof(GraphServiceClient)).ShouldBe(1);
    }
}

public class GraphMailServiceTests
{
    private static GraphServiceClient FakeClient() =>
        new(new Azure.Identity.ClientSecretCredential(
            "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000", "fake-secret"));

    [Test]
    public void SendAsync_SenderNotAllowed_ThrowsBeforeAnyNetworkCall()
    {
        var sut = new GraphMailService(FakeClient(), new[] { "noreply@contoso.com" });

        var ex = Assert.Throws<ArgumentException>(() =>
            sut.SendAsync("ceo@contoso.com", ["someone@contoso.com"], "s", "c"));

        ex!.ParamName.ShouldBe("from");
    }

    [Test]
    public void AddMicrosoftGraph_AllowedSenders_AreEnforcedByResolvedService()
    {
        var services = new ServiceCollection();
        services.AddMicrosoftGraph(o =>
        {
            o.TenantId = "00000000-0000-0000-0000-000000000000";
            o.ClientId = "00000000-0000-0000-0000-000000000000";
            o.ClientSecret = "fake-secret";
            o.AllowedSenders = ["noreply@contoso.com"];
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IGraphMailService>();

        Assert.Throws<ArgumentException>(() =>
            sut.SendAsync("other@contoso.com", ["someone@contoso.com"], "s", "c"));
    }

    [Test]
    public void FileAttachment_SerializesAsBase64ContentBytes_WithFileAttachmentODataType()
    {
        var bytes = Encoding.UTF8.GetBytes("hello world");
        var attachment = new FileAttachment
        {
            OdataType = "#microsoft.graph.fileAttachment",
            Name = "a.txt",
            ContentBytes = bytes,
        };

        using var writer = new JsonSerializationWriter();
        writer.WriteObjectValue(null, attachment);
        using var stream = writer.GetSerializedContent();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        Assert.That(json, Does.Contain("\"@odata.type\":\"#microsoft.graph.fileAttachment\""));
        Assert.That(json, Does.Contain($"\"contentBytes\":\"{Convert.ToBase64String(bytes)}\""));
        Assert.That(json, Does.Contain("\"name\":\"a.txt\""));
    }
}
