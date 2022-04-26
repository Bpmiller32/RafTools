// See https://aka.ms/new-console-template for more information
using System.Globalization;
using CsvHelper;
using EmailReader.App;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;

Console.WriteLine("Hello, World!");
var commandLineArgs = Environment.GetCommandLineArgs();
var partner = Environment.GetCommandLineArgs()[1];
int year = int.Parse(Environment.GetCommandLineArgs()[2]);

using (var client = new ImapClient())
{
    client.Connect(@"outlook.office365.com", 993, true);
    client.Authenticate(@"billy.miller@raf.com", @"wwnhkmhjctfttfjf");

    client.Inbox.Open(FolderAccess.ReadOnly);

    DateTime startTime = new DateTime(year, 1, 1);
    DateTime endTime = new DateTime(year, 12, 31);
    BinarySearchQuery query = SearchQuery
                                .FromContains(@"@" + partner + @".de")
                                .And(SearchQuery.DeliveredAfter(startTime))
                                .And(SearchQuery.DeliveredBefore(endTime));
    IList<UniqueId> uids = client.Inbox.Search(query);


    List<SupportCase> supportCases = new List<SupportCase>();
    List<SupportCase> possibleDupCases = new List<SupportCase>();

    foreach (var uid in uids)
    {
        MimeMessage email = client.Inbox.GetMessage(uid);
        string scrubbedSubject;
        if (email.Subject.Contains(@"RE"))
        {
            // Contains a RE:
            scrubbedSubject = email.Subject.Substring(4, email.Subject.Length - 4);
        }
        else
        {
            scrubbedSubject = email.Subject;
        }


        bool unique = true;
        foreach (var supportCase in supportCases)
        {
            if (scrubbedSubject == supportCase.Subject)
            {
                // Not unique
                unique = false;
            }            
        }

        if (unique == true)
        {
            SupportCase newCase = new SupportCase()
            {
                DateRecieved = email.Date,
                Sender = email.From.Mailboxes.ToList()[0].Address,
                Subject = scrubbedSubject
            };
            supportCases.Add(newCase);
        }
        else
        {
            SupportCase newCase = new SupportCase()
            {
                DateRecieved = email.Date,
                Sender = email.From.Mailboxes.ToList()[0].Address,
                Subject = scrubbedSubject
            };
            possibleDupCases.Add(newCase);
        }
    }

    using (var writer = new StreamWriter(@"C:\Users\billy\Desktop\" + partner + year + @".csv"))
    {
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            var totalEmailCorrespondence = new {text = "TotalEmailCorrespondence", totalCorrespondence = uids.Count};
            csv.WriteRecord(totalEmailCorrespondence);
            csv.NextRecord();
            csv.WriteRecords(supportCases);
            csv.NextRecord();
            csv.NextRecord();
            csv.WriteRecords(possibleDupCases);
        }
    }

    client.Disconnect(true);
}