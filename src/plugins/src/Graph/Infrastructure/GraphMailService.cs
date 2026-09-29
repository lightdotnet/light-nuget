using Light.Graph;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Infrastructure
{
    /// <summary>
    /// <see cref="IGraphMailService"/> that sends mail via <c>POST /users/{from}/sendMail</c> using app-only auth.
    /// </summary>
    /// <remarks>
    /// With the <c>Mail.Send</c> <b>application</b> permission the app can send as <b>any</b> mailbox in the
    /// tenant, so whatever reaches the <c>from</c> argument decides the sender. Scope the app registration with
    /// an Exchange Online <c>ApplicationAccessPolicy</c> (or RBAC for Applications), never pass untrusted input
    /// as <c>from</c>, and optionally set <see cref="GraphOptions.AllowedSenders"/> as an extra client-side check.
    /// </remarks>
    public class GraphMailService : IGraphMailService
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly HashSet<string>? _allowedSenders;

        public GraphMailService(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        /// <param name="graphServiceClient">The Graph client used to send mail.</param>
        /// <param name="allowedSenders">
        /// Mailboxes <see cref="SendAsync"/> may send as (case-insensitive). <see langword="null"/> or empty = no restriction.
        /// </param>
        public GraphMailService(GraphServiceClient graphServiceClient, IEnumerable<string>? allowedSenders)
            : this(graphServiceClient)
        {
            if (allowedSenders != null)
            {
                var set = new HashSet<string>(
                    allowedSenders.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()),
                    StringComparer.OrdinalIgnoreCase);

                if (set.Count > 0)
                {
                    _allowedSenders = set;
                }
            }
        }

        private List<Recipient> RecipientBuilder(List<string> addresses)
        {
            // generate recipients
            return addresses
                .Select(address => new Recipient
                {
                    EmailAddress = new EmailAddress { Address = address }
                })
                .ToList();
        }

        private List<Attachment> AttachmentBuilder(Dictionary<string, byte[]> attachments)
        {
            // FileAttachment.ContentBytes is serialized by Kiota as a base64 string under "contentBytes",
            // identical to the previous AdditionalData["contentBytes"] = Convert.ToBase64String(...) payload.
            return attachments
                .Select(s => (Attachment)new FileAttachment
                {
                    OdataType = "#microsoft.graph.fileAttachment",
                    Name = s.Key,
                    ContentBytes = s.Value,
                })
                .ToList();
        }

        public Task SendAsync(
            string from,
            List<string> recipients,
            string subject,
            string content,
            List<string>? ccRecipients = null,
            List<string>? bccRecipients = null,
            Dictionary<string, byte[]>? attachments = null,
            CancellationToken cancellationToken = default)
        {
            if (_allowedSenders != null && (from is null || !_allowedSenders.Contains(from.Trim())))
            {
                throw new ArgumentException(
                    $"Sender '{from}' is not in {nameof(GraphOptions)}.{nameof(GraphOptions.AllowedSenders)}.",
                    nameof(from));
            }

            // Define a simple e-mail message.
            var message = new Message
            {
                ToRecipients = RecipientBuilder(recipients),
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = content
                },
            };

            if (ccRecipients != null)
            {
                // add CC
                message.CcRecipients = RecipientBuilder(ccRecipients);
            }

            if (bccRecipients != null)
            {
                // add BCC
                message.BccRecipients = RecipientBuilder(bccRecipients);
            }

            if (attachments != null)
            {
                // add attachments
                message.Attachments = AttachmentBuilder(attachments);
            }

            var request = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = true,
            };

            // Send mail as the given user (see class remarks: requires ApplicationAccessPolicy scoping).
            return _graphServiceClient.Users[from].SendMail.PostAsync(request, cancellationToken: cancellationToken);
        }
    }
}
