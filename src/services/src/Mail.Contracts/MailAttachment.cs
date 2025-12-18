namespace Light.Mail
{
    public class MailAttachment
    {
        public MailAttachment(string fileName, byte[] fileToBytes)
        {
            FileName = fileName;
            FileToBytes = fileToBytes;
        }

        public string FileName { get; set; }

        public byte[] FileToBytes { get; set; }
    }
}
