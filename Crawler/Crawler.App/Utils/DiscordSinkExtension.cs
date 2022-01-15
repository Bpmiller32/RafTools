using System;
using Serilog;
using Serilog.Configuration;

namespace Crawler.App
{
    public static class DiscordSinkExtension
    {
        public static LoggerConfiguration DiscordSink(this LoggerSinkConfiguration loggerConfiguration)
        {
            return loggerConfiguration.Sink(new DiscordSink());
        }
    }
}
