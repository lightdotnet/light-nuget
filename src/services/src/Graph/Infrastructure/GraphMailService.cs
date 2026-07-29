using Light.Mail;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Graph.Infrastructure
{
    public class GraphMailService : IGraphMailService
    {
        private readonly GraphServiceClient _graphServiceClient;

        public GraphMailService(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        public async Task SendAsync(MailFrom from, MailMessage mail, CancellationToken cancellationToken = default)
        {
            // Define a simple e-mail message.
            var message = new Message
            {
                ToRecipients = RecipientBuilder(mail.Recipients),
                Subject = mail.Subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = mail.Content
                },
            };

            if (mail.CcRecipients != null)
            {
                // add CC
                message.CcRecipients = RecipientBuilder(mail.CcRecipients);
            }

            if (mail.BccRecipients != null)
            {
                // add BCC
                message.BccRecipients = RecipientBuilder(mail.BccRecipients);
            }

            if (mail.Attachments != null)
            {
                // add attachments
                message.Attachments = AttachmentBuilder(mail.Attachments);
            }

            var request = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = true,
            };

            // Send mail as the given user. 
            await _graphServiceClient.Users[from.Address].SendMail.PostAsync(request, cancellationToken: cancellationToken);
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

        private List<Attachment> AttachmentBuilder(List<MailAttachment> attachments)
        {
            return attachments
                .Select(s => new Attachment
                {
                    OdataType = "#microsoft.graph.fileAttachment",
                    Name = s.FileName,
                    AdditionalData = new Dictionary<string, object>
                    {
                        {
                            "contentBytes" , Convert.ToBase64String(s.FileToBytes)
                        },
                    }
                })
                .ToList();
        }
    }
}
