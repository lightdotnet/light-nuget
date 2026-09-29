using System.Text.Json;
using Elastic.CommonSchema.Serilog;
using Elastic.Ingest.Elasticsearch.Serialization;
using Light.Serilog;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace UnitTests.SerilogTests;

public class ElasticsearchDailyIndexSinkTests
{
    // Offline: the transport points at an unused local address and is never called by these tests.
    private static readonly Elastic.Transport.ITransport Transport =
        ElasticsearchDailyIndexSink.CreateTransport([new Uri("http://localhost:9")], "user", "pass");

    private static string IndexFor(string service, string environment, DateTimeOffset timestamp)
    {
        var options = ElasticsearchDailyIndexSink.CreateChannelOptions(Transport, service, environment);
        var document = new LogEventEcsDocument { Timestamp = timestamp };

        // Same routine IndexChannel uses to build each bulk operation's target index.
        return BulkRequestDataFactory.CreateBulkOperationHeaderForIndex(document, options).Index!;
    }

    [Test]
    public void BuildIndexFormat_KeepsOldLayout_LowercasesParts_AndKeepsDashes()
    {
        ElasticsearchDailyIndexSink.BuildIndexFormat("WTCVN-Api", "Production")
            .ShouldBe("wtcvn-api-production-{0:yyyy-MM-dd}-generic-default");
    }

    [Test]
    public void EventJustBeforeMidnightUtc_GoesToThatDaysIndex()
    {
        IndexFor("WTCVN-Api", "Production", new DateTimeOffset(2026, 9, 29, 23, 59, 0, TimeSpan.Zero))
            .ShouldBe("wtcvn-api-production-2026-09-29-generic-default");
    }

    [Test]
    public void EventAtMidnightUtc_GoesToNextDaysIndex()
    {
        IndexFor("WTCVN-Api", "Production", new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero))
            .ShouldBe("wtcvn-api-production-2026-09-30-generic-default");
    }

    [Test]
    public void NonUtcTimestamp_IsBucketedByItsUtcDate()
    {
        // 2026-09-30 06:30 at +07:00 == 2026-09-29 23:30 UTC
        IndexFor("api", "dev", new DateTimeOffset(2026, 9, 30, 6, 30, 0, TimeSpan.FromHours(7)))
            .ShouldBe("api-dev-2026-09-29-generic-default");
    }

    [TestCase("My App/v2", "Staging EU", "my_app_v2-staging_eu-{0:yyyy-MM-dd}-generic-default")]
    [TestCase("_svc", "", "svc-development-{0:yyyy-MM-dd}-generic-default")]
    [TestCase("", "Prod", "unknownapp-prod-{0:yyyy-MM-dd}-generic-default")]
    [TestCase("a{0}b", "x", "a_0_b-x-{0:yyyy-MM-dd}-generic-default")]
    public void BuildIndexFormat_ReplacesOnlyForbiddenCharacters(string service, string environment, string expected)
    {
        ElasticsearchDailyIndexSink.BuildIndexFormat(service, environment).ShouldBe(expected);
    }

    [Test]
    public void BuildIndexFormat_UsesInvariantCulture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
            ElasticsearchDailyIndexSink.BuildIndexFormat("INVOICE", "Staging")
                .ShouldBe("invoice-staging-{0:yyyy-MM-dd}-generic-default");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Test]
    public void ToDocument_MapsLogEventToEcs_WithoutElasticsearch()
    {
        var timestamp = new DateTimeOffset(2026, 9, 29, 23, 59, 0, TimeSpan.Zero);
        var logEvent = new LogEvent(
            timestamp,
            LogEventLevel.Warning,
            new InvalidOperationException("boom"),
            new MessageTemplateParser().Parse("Hello {Name}"),
            [new LogEventProperty("Name", new ScalarValue("World"))]);

        var document = ElasticsearchDailyIndexSink.ToDocument(logEvent, new EcsTextFormatterConfiguration<LogEventEcsDocument>());

        document.Timestamp.ShouldBe(timestamp);
        document.Message.ShouldBe("Hello \"World\"");
        document.Log!.Level.ShouldBe("Warning");
        Assert.That(document.Error!.Message, Is.EqualTo("boom"));
        Assert.That(ReferenceEquals(document.LogEvent, logEvent), Is.True);
    }

    [Test]
    public void IndexTemplate_IsPlainIndex_AtHigherPriorityThanStockEcs_AndMatchesDailyIndices()
    {
        var options = ElasticsearchDailyIndexSink.CreateChannelOptions(Transport, "wtcvn-api", "production");
        using var channel = new ElasticsearchDailyIndexSink.DailyEcsIndexChannel(options);

        channel.IndexTemplatePattern.ShouldBe("wtcvn-api-production-*-generic-default");

        var stock = JsonDocument.Parse(Elastic.CommonSchema.Elasticsearch.IndexTemplates
            .GetIndexTemplateForElasticsearchComposable(channel.IndexTemplatePattern)).RootElement;
        using var ours = JsonDocument.Parse(
            ElasticsearchDailyIndexSink.DailyEcsIndexChannel.BuildIndexTemplate(channel.IndexTemplatePattern));

        Assert.That(ours.RootElement.TryGetProperty("data_stream", out _), Is.False);
        ours.RootElement.GetProperty("priority").GetInt64()
            .ShouldBe(stock.GetProperty("priority").GetInt64() + 1);
        ours.RootElement.GetProperty("index_patterns")[0].GetString()
            .ShouldBe("wtcvn-api-production-*-generic-default");
    }
}
