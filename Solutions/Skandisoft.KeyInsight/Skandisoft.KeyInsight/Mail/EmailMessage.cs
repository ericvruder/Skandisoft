using System;
using System.Collections.Generic;
using System.Text;

namespace Skandisoft.KeyInsight.Sources.Mail
{
    internal class EmailMessage
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string Body { get; set; }
        public string Subject { get; set; }
        public DateTimeOffset Date { get; set; }
        public List<string> Attachments { get; set; }
        public List<EmailLink> Links { get; set; }
    }
}
