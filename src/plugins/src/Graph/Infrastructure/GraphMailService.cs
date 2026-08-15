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
    public class GraphMailService : IGraphMailService
    {
        private readonly GraphServiceClient _graphServiceClient;

        public GraphMailService(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
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
            return attachments
                .Select(s => new Attachment
                {
                    OdataType = "#microsoft.graph.fileAttachment",
                    Name = s.Key,
                    AdditionalData = new Dictionary<string, object>
                    {
                        {
                            "contentBytes" , Convert.ToBase64String(s.Value)
                        },
                    }
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

            // Send mail as the given user. 
            return _graphServiceClient.Users[from].SendMail.PostAsync(request, cancellationToken: cancellationToken);
        }
    }
}
