using System;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Skandisoft.KeyInsight.Sources.Mail
{
    internal class FetchNewEmails
    {
        private readonly ILogger log;
        private readonly IMailClient client;
        private readonly Lazy<Task<Container>> lazyContainer;

        public FetchNewEmails(ILogger<FetchNewEmails> log, IMailClient client, CosmosClient dbClient)
        {
            this.log = log;
            this.client = client;
            this.lazyContainer = new Lazy<Task<Container>>(() => CreateContainer(dbClient));
        }

        private async Task<Container> CreateContainer(CosmosClient dbClient)
        {
            var db = await dbClient.CreateDatabaseIfNotExistsAsync("Sources");
            var container = await db.Database.CreateContainerIfNotExistsAsync("Emails", "/from");

            return container.Container;
        }

        [FunctionName("FetchNewEmails")]
        public async Task Run([TimerTrigger("0 6 * * *")]TimerInfo myTimer)
        {            
            log.LogInformation("Fetching new emails, past due: {pastdue}", myTimer.IsPastDue);

            await foreach (var msg in client.FetchNewMessages())
            {                            
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(msg.Body);

                var links = htmlDoc.DocumentNode
                    .SelectNodes("//a[@href]")?
                    .Select(x => new EmailLink(x.InnerText, x.Attributes["href"].Value))
                    .ToList();

                var email = new EmailMessage
                {
                    Id = msg.UniqueId,
                    From = msg.From,
                    Attachments = msg.Attachments,
                    Subject = msg.Subject,
                    Body = msg.Body,
                    Date = msg.Date, 
                    Links = links
                };

                var container = await lazyContainer.Value;

                var result = await container.CreateItemAsync(email);

                log.LogInformation("Processed msg {message} from {from}, used {ru} ru", msg.UniqueId, msg.From, result.RequestCharge);
            }
        }
    }
}
