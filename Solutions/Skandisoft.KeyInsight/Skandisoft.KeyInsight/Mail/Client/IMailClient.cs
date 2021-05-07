using System;
using System.Collections.Generic;
using System.Text;

namespace Skandisoft.KeyInsight.Sources.Mail
{
    internal interface IMailClient
    {
        IAsyncEnumerable<Email> FetchNewMessages();
    }
}
