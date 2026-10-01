using Elastic.Channels;
using Elastic.CommonSchema;
using Elastic.CommonSchema.Elasticsearch;
using Elastic.CommonSchema.Serialization;
using Elastic.CommonSchema.Serilog;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.CommonSchema;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Ingest.Elasticsearch.Serialization;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Light.Serilog
{
    /// <summary>
    /// Serilog sink that writes ECS documents to one Elasticsearch data stream <b>per UTC day of each event</b>:
    /// <c>{service}-{environment}-{yyyy-MM-dd}-generic-default</c>. The index name is computed per document from
    /// its <c>@timestamp</c> (<see cref="IndexChannelOptions{TEvent}.TimestampLookup"/> + UTC
    /// <see cref="IndexChannelOptions{TEvent}.IndexOffset"/>), so it rolls over at 00:00 UTC without a restart.
    /// </summary>
    internal sealed class ElasticsearchDailyIndexSink : ILogEventSink, IDisposable
    {
        private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(10);

        private readonly EcsTextFormatterConfiguration<LogEventEcsDocument> _formatterConfiguration;
        private readonly DailyEcsIndexChannel _channel;

        public ElasticsearchDailyIndexSink(
            IndexChannelOptions<LogEventEcsDocument> channelOptions,
            EcsTextFormatterConfiguration<LogEventEcsDocument> formatterConfiguration,
            BootstrapMethod bootstrapMethod)
        {
            _formatterConfiguration = formatterConfiguration;
            _channel = new DailyEcsIndexChannel(channelOptions);

            try
            {
                // With BootstrapMethod.Failure this throws if the templates can't be installed (same as before).
                _channel.BootstrapElasticsearch(bootstrapMethod);
            }
            catch
            {
                _channel.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Index name format: <c>{service}-{environment}-{0:yyyy-MM-dd}-generic-default</c>. Service and environment
        /// are lower-cased (<see cref="string.ToLowerInvariant"/>); only characters Elasticsearch forbids in index names
        /// (and format braces) are replaced with <c>_</c> — <c>-</c> is kept (e.g. <c>wtcvn-api</c>).
        /// The <c>-generic-default</c> suffix keeps the exact name the previous data-stream based sink produced.
        /// </summary>
        internal static string BuildIndexFormat(string serviceName, string environment) =>
            $"{SanitizeIndexPart(serviceName, "unknownapp")}-{SanitizeIndexPart(environment, "development")}-{{0:yyyy-MM-dd}}-generic-default";

        internal static IndexChannelOptions<LogEventEcsDocument> CreateChannelOptions(
            ITransport transport, string serviceName, string environment)
        {
            return new IndexChannelOptions<LogEventEcsDocument>(transport)
            {
                IndexFormat = BuildIndexFormat(serviceName, environment),
                // route each document by its own event time, in UTC (matches the old DateTime.UtcNow date)
                TimestampLookup = d => d.Timestamp,
                IndexOffset = TimeSpan.Zero,
                // data streams only accept 'create' ops
                OperationMode = OperationMode.Create,
                SerializerContext = EcsJsonContext.Default,
                BufferOptions = new BufferOptions(),
                ExportExceptionCallback = e => SelfLog.WriteLine("Elasticsearch export failed: {0}", e),
                ExportMaxRetriesCallback = docs => SelfLog.WriteLine(
                    "Elasticsearch export gave up on {0} event(s) after max retries.", docs.Count),
                ServerRejectionCallback = rejected => SelfLog.WriteLine(
                    "Elasticsearch rejected {0} event(s), first: {1}", rejected.Count,
                    rejected.Select(r => r.Item2.Error?.ToString()).FirstOrDefault()),
            };
        }

        internal static ITransport CreateTransport(IEnumerable<Uri> endpoints, string username, string password)
        {
            var pool = new StaticNodePool(endpoints.Select(e => new Node(e)), true);
            var configuration = new TransportConfigurationDescriptor(pool, null, null, ElasticsearchProductRegistration.Default)
                .Authentication(new BasicAuthentication(username, password));

            return new DistributedTransport(configuration);
        }

        internal static LogEventEcsDocument ToDocument(LogEvent logEvent, EcsTextFormatterConfiguration<LogEventEcsDocument> configuration)
        {
            var document = LogEventConverter.ConvertToEcs(logEvent, configuration);
            document.LogEvent = logEvent;
            return document;
        }

        public void Emit(LogEvent logEvent)
        {
            if (!_channel.TryWrite(ToDocument(logEvent, _formatterConfiguration)))
            {
                SelfLog.WriteLine("Failed to push log event to the Elasticsearch channel (buffer full or closed).");
            }
        }

        /// <summary>Flushes buffered events (bounded wait) and disposes the channel.</summary>
        public void Dispose()
        {
            try
            {
                // run the drain off the caller's SynchronizationContext (WPF/WinForms) so blocking on it can't deadlock
                Task.Run(() => _channel.WaitForDrainAsync(DrainTimeout).AsTask()).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                SelfLog.WriteLine("Elasticsearch sink drain on dispose failed: {0}", e);
            }

            _channel.Dispose();
        }

        private static string SanitizeIndexPart(string? value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            var builder = new StringBuilder(value!.Trim().ToLowerInvariant());
            for (var i = 0; i < builder.Length; i++)
            {
                switch (builder[i])
                {
                    // forbidden in Elasticsearch index names, plus ':' (deprecated) and string.Format braces
                    case '\\': case '/': case '*': case '?': case '"': case '<': case '>':
                    case '|': case ' ': case ',': case '#': case ':': case '{': case '}':
                        builder[i] = '_';
                        break;
                }
            }

            // index names may not start with '-', '_' or '+'
            var result = builder.ToString().TrimStart('-', '_', '+');
            return result.Length > 0 ? result : fallback;
        }

        /// <summary>
        /// <see cref="EcsIndexChannel{TEcsDocument}"/> whose bootstrap installs the ECS data-stream template for
        /// <c>{service}-{env}-*-generic-default</c> at a priority one above the stock ECS templates.
        /// </summary>
        /// <remarks>
        /// Why not the stock bootstrap: the previous sink installed data-stream templates such as
        /// <c>{service}-{env}-2026-09-29-generic-*</c> at the same ECS priority, and Elasticsearch refuses a new
        /// template whose patterns overlap an existing one at equal priority — which would fail startup under
        /// <see cref="BootstrapMethod.Failure"/>.
        /// </remarks>
        internal sealed class DailyEcsIndexChannel : EcsIndexChannel<LogEventEcsDocument>
        {
            public DailyEcsIndexChannel(IndexChannelOptions<LogEventEcsDocument> options) : base(options)
            {
            }

            internal string IndexTemplateName => TemplateName + "-" + EcsDocument.Version;

            internal string IndexTemplatePattern => TemplateWildcard;

            public override bool BootstrapElasticsearch(BootstrapMethod bootstrapMethod, string? ilmPolicy = null)
            {
                if (bootstrapMethod == BootstrapMethod.None)
                {
                    return true;
                }

                if (IndexTemplateExists(IndexTemplateName))
                {
                    return false;
                }

                foreach (var component in IndexComponents.Components)
                {
                    if (!PutComponentTemplate(bootstrapMethod, component.Key, component.Value))
                    {
                        return false;
                    }
                }

                return PutIndexTemplate(bootstrapMethod, IndexTemplateName, BuildIndexTemplate(IndexTemplatePattern));
            }

            /// <summary>
            /// Priority used if the stock template ever comes without a numeric <c>priority</c>:
            /// the ECS.NET 9.0.0 stock priority (589824) + 1.
            /// </summary>
            internal const long FallbackPriority = 589825;

            /// <summary>
            /// Resolves each document's target index under <see cref="CultureInfo.InvariantCulture"/>. The stock
            /// <see cref="BulkRequestDataFactory.CreateBulkOperationHeaderForIndex{TEvent}"/> uses <c>string.Format</c>
            /// with the current culture, so e.g. th-TH (Buddhist calendar) would write to <c>...-2569-09-29-...</c>
            /// and fa-IR/ar-SA to Persian/Hijri dates instead of the Gregorian <c>yyyy-MM-dd</c> UTC date.
            /// </summary>
            protected override BulkOperationHeader CreateBulkOperationHeader(LogEventEcsDocument @event)
            {
                // swapping the culture (rather than rebuilding the header) keeps every other header field produced by the base
                var previous = CultureInfo.CurrentCulture;

                // the date part is a custom "yyyy-MM-dd" pattern ('-' is a literal, digits are never localized), so only a
                // non-Gregorian calendar changes the output; skip the per-event culture swap in the common case
                if (ReferenceEquals(previous, CultureInfo.InvariantCulture) || previous.DateTimeFormat.Calendar is GregorianCalendar)
                {
                    return base.CreateBulkOperationHeader(@event);
                }

                try
                {
                    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                    return base.CreateBulkOperationHeader(@event);
                }
                finally
                {
                    CultureInfo.CurrentCulture = previous;
                }
            }

            /// <summary>Exposes <see cref="CreateBulkOperationHeader"/> to tests.</summary>
            internal BulkOperationHeader GetBulkOperationHeader(LogEventEcsDocument document) => CreateBulkOperationHeader(document);

            /// <summary>
            /// The stock ECS composable template (including <c>data_stream</c>) with
            /// <c>priority</c> = stock priority + 1 (<see cref="FallbackPriority"/> if the stock template has none).
            /// </summary>
            /// <remarks>
            /// <c>data_stream</c> must be kept: the daily names created by the previous sink are data streams, and
            /// Elasticsearch rejects (400 <c>illegal_argument_exception</c>) a higher-priority template without a data
            /// stream configuration that would make existing data streams "no longer match a data stream template".
            /// </remarks>
            internal static string BuildIndexTemplate(string indexPattern)
            {
                var template = JsonNode.Parse(IndexTemplates.GetIndexTemplateForElasticsearchComposable(indexPattern))!.AsObject();

                // each daily name stays a data stream (as with the previous sink); 'create' ops auto-create it
                template["data_stream"] ??= new JsonObject();

                // one above the stock ECS priority so it never ties with the old per-date data-stream templates
                template["priority"] = template["priority"] is JsonValue value && value.TryGetValue<long>(out var priority)
                    ? priority + 1
                    : FallbackPriority;

                return template.ToJsonString();
            }
        }
    }
}
