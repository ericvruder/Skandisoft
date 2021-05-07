using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skandisoft.KeyInsight.Sources.Mail
{
    class MailClient : IMailClient
    {
        private readonly IOptions<EmailConfiguration> options;

        public MailClient(IOptions<EmailConfiguration> options)
        {
            this.options = options;
        }

        public async IAsyncEnumerable<Email> FetchNewMessages()
        {
            var conf = options.Value;

            using var client = new ImapClient();
            await client.ConnectAsync(conf.Hostname);
            await client.AuthenticateAsync(conf.Username, conf.Password);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            var unread = await inbox.SearchAsync(SearchQuery.NotSeen);                                    
            var items = client.Inbox.Fetch(unread, MessageSummaryItems.UniqueId | MessageSummaryItems.BodyStructure);

            foreach (var uid in unread)
            {
                var msg = await inbox.GetMessageAsync(uid);

                var atts = msg.Attachments
                    .Select(x => x.ContentDisposition?.FileName ?? "<MISSING>")
                    .ToList();

                yield return new Email
                {
                    Date = msg.Date,
                    UniqueId = uid.ToString(),
                    From = msg.From.Mailboxes.First()?.Address ?? "<MISSING>",
                    Body = msg.HtmlBody,
                    Subject = msg.Subject,
                    Attachments = atts
                };

                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
            }            
        }
    }
}
