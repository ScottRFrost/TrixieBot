//// To build: dotnet publish -r win10-x64

using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace TrixieBot
{
    public class TrixieBot
    {
        public Config Config { get; set; }
        public Keys Keys { get; set; }

        public TrixieBot()
        {
            // Read Config
            Config = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText("config.json"));
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Configuation read");
        }

        public async Task<bool> Start()
        {
            // Start Telegram
            Task<bool> telegram;
            if (Config.Keys.TelegramKey != string.Empty)
            {
                var telegramProtocol = new TelegramProtocol(Config);
                telegram = telegramProtocol.Start();
            }
            else
            {
                telegram = Task.FromResult(false);
            }

            // Start Discord
            Task<bool> discord;
            if (Config.Keys.DiscordToken != string.Empty)
            {
                var discordProtocol = new DiscordProtocol(Config);
                discord = discordProtocol.Start();
            }
            else
            {
                discord = Task.FromResult(false);
            }

            // Wait for all tasks to end (which shouldn't ever happen)
            await Task.WhenAll(telegram, discord).ConfigureAwait(false);
            return false;
        }
    }

    #region "Config Structs"
    public struct Keys
    {
        public string TelegramKey { get; set; }
        public string DiscordToken { get; set; }
        public string WundergroundKey { get; set; }
        public string BingKey { get; set; }
        public string WolframAppID { get; set; }
        public string BattleNetKey { get; set; }
        public string CoinBaseAPIKey { get; set; }
        public string CoinBaseAPISecret { get; set; }
        public string BittrexAPIKey { get; set; }
        public string BittrexAPISecret { get; set; }
    }

    public struct Rss
    {
        public string URL { get; set; }
        public string Format { get; set; } // "RSS" or "Atom"
        public string Type { get; set; } // "Latest" to only send the top item, or "All" to send all items since the MostRecent pull
        public string Protocol { get; set; }
        public string Destination { get; set; }
        public DateTime MostRecent { get; set; }
    }

    public struct Config
    {
        public Keys Keys { get; set; }
        public Rss[] Rss { get; set; }
    }
    #endregion

    #region "RSS & Atom Structs"
    public struct RSSItem
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public string Guid { get; set; }
        public DateTime? PubDate { get; set; }
    }

    public struct AtomEntry
    {
        public string Category { get; set; }
        public string Content { get; set; }
        public string Id { get; set; }
        public string Link { get; set; }
        public DateTime Updated { get; set; }
        public string Title { get; set; }
    }
    #endregion
}