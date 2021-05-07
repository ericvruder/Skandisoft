using System;
using System.Collections.Generic;
using System.Text;

namespace Skandisoft.KeyInsight.Sources.Mail
{
    internal class EmailLink
    {
        public string Text { get; set; }
        public string Link { get; set; }

        public EmailLink(string text, string link)
        {
            Text = text;
            Link = link;
        }
    }
}
