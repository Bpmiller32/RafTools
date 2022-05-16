// using Serilog.Core;
// using Serilog.Events;
// using JNogueira.Discord.Webhook.Client;

// namespace Crawler.App
// {
//     public class DiscordSink : ILogEventSink
//     {
//         public void Emit(LogEvent logEvent)
//         {
//             if ((logEvent.Exception != null) || (ShouldLogMessage(LogEventLevel.Warning, logEvent.Level) == false)) 
//             {
//                 return;
//             }

//             var client = new DiscordWebhookClient(@"https://discord.com/api/webhooks/799379913458843710/XytHRu3A8dX-1hXWvVvGKUBRjnf43rWbkcn4OoTacVAxzDaCEtYqRs4hxS91HVN53-J0");
//             var message = new DiscordMessage(logEvent.MessageTemplate.Text, "Crawler", null, false, null);

//             client.SendToDiscord(message).GetAwaiter().GetResult();
//         }

//         private static bool ShouldLogMessage(LogEventLevel minimumLogEventLevel, LogEventLevel messageLogEventLevel)
//         {
//             if ((int)minimumLogEventLevel > (int)messageLogEventLevel)
//             {
//                 return false;
//             }
//             return true;
//         }
//     }
// }
