using System.Collections.Generic;

namespace Light.Mail
{
    public class MailMessage
    {
        public List<string> Recipients { get; set; } = null!;

        public string Subject { get; set; } = default!;

        public string Content { get; set; } = default!;

        public List<string>? CcRecipients { get; set; }

        public List<string>? BccRecipients { get; set; }

        public List<MailAttachment>? Attachments { get; set; }
    }
}
