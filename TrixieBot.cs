//// To build: dotnet publish -r win10-x64

using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace TrixieBot
{
    public class TrixieBot
    {
        public Config config;
        public Keys keys;

        public TrixieBot()
        {
            // Read Config
            config = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText("config.json"));
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Configuation read");
        }

        public async Task<bool> Start()
        {
            // Start Telegram
            Task<bool> telegram;
            if (config.Keys.TelegramKey != string.Empty)
            {
                var telegramProtocol = new TelegramProtocol(config);
                telegram = telegramProtocol.Start();
            }
            else
            {
                telegram = Task.FromResult(false);
            }

            // Start Discord
            Task<bool> discord;
            if (config.Keys.DiscordToken != string.Empty)
            {
                var discordProtocol = new DiscordProtocol(config);
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
        public string TelegramKey;
        public string DiscordToken;
        public string WundergroundKey;
        public string BingKey;
        public string WolframAppID;
        public string BattleNetKey;
        public string CoinBaseAPIKey;
        public string CoinBaseAPISecret;
        public string BittrexAPIKey;
        public string BittrexAPISecret;
    }

    public struct Rss
    {
        public string URL;
        public string Format; // "RSS" or "Atom"
        public string Type; // "Latest" to only send the top item, or "All" to send all items since the MostRecent pull
        public string Protocol;
        public string Destination;
        public DateTime MostRecent;
    }

    public struct Config
    {
        public Keys Keys;
        public Rss[] Rss;
    }
    #endregion

    #region "RSS & Atom Structs"
    public struct RSSItem
    {
        public string Title;
        public string Link;
        public string Description;
        public string Guid;
        public DateTime? PubDate;
    }

    public struct AtomEntry
    {
        public string Category;
        public string Content;
        public string Id;
        public string Link;
        public DateTime Updated;
        public string Title;
    }
    #endregion
}