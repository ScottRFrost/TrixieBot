using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Globalization;

namespace TrixieBot
{
    public class DiscordProtocol : BaseProtocol
    {
        private DiscordSocketClient bot;

        public DiscordProtocol(Config config) : base(config)
        {
            this.config = config;
        }

        public override async Task<bool> Start()
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Discord Protocol starting...");
            bot = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info
            });

            // Hook up logging
            bot.Log += Log;

            // Hook up message handling
            bot.MessageReceived += MessageReceived;

            // Log in and start bot
            await bot.LoginAsync(TokenType.Bot, config.Keys.DiscordToken);
            await bot.StartAsync();

            // Start RSS / Atom processing if there are any items for this protocol
            var startTimer = false;
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Checking for Discord RSS...");
            if (config.Rss.Length > 0)
            {
                foreach (var rss in config.Rss)
                {
                    if (rss.Protocol.ToUpperInvariant() == "DISCORD")
                    {
                        Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " At least one Discord RSS found starting timer...");
                        startTimer = true;
                        break;
                    }
                }
            }

            // Timer
            if(startTimer)
            {
                while(true)
                {
                    OnTimerTick(null);
                    Thread.Sleep(900000);
                }
            }

            // Wait forever
            await Task.Delay(-1); 
            return true;
        }

        public async void OnTimerTick(object configObject)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Checking Discord RSS / Atom Feeds...");

            // Read Config
            var config = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText("config.json"));

            // Parse each feed
            var writeConfig = false;
            for (var thisRss = 0; thisRss < config.Rss.Length; thisRss++)
            {
                if (config.Rss[thisRss].Protocol.ToUpperInvariant() == "DISCORD")
                {
                    var newMostRecent = config.Rss[thisRss].MostRecent;
                    var numFound = 0;
                    if (config.Rss[thisRss].Format.ToUpperInvariant() == "RSS")
                    {
                        // Parse RSS Format
                        try
                        {
                            var xDocument = XDocument.Load(config.Rss[thisRss].URL);
                            foreach (var xElement in xDocument.Root.Descendants().First(i => i.Name.LocalName == "channel").Elements().Where(i => i.Name.LocalName == "item"))
                            {
                                // Parse xDocument to POCO
                                var rssItem = new RSSItem
                                {
                                    Title = xElement.Elements().First(i => i.Name.LocalName == "title").Value,
                                    Link = xElement.Elements().First(i => i.Name.LocalName == "link").Value,
                                    Description = xElement.Elements().First(i => i.Name.LocalName == "description").Value,
                                };

                                // Dates in RSS are often in some retarded format
                                var dateString = xElement.Elements().First(i => i.Name.LocalName == "pubDate").Value;
                                dateString = dateString.Replace("UTC", "+00:00").Replace("GMT", "+00:00").Replace("EST", "-05:00").Replace("EDT", "-04:00").Replace("CST", "-06:00").Replace("CDT", "-05:00").Replace("MST", "-07:00").Replace("MDT", "-06:00").Replace("PST", "-08:00").Replace("PDT", "-07:00");  // Hack for time zone names instead of UTC offsets
                                try
                                {
                                    // Parse the dates using the standard universal date format
                                    rssItem.PubDate = DateTime.Parse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                                }
                                catch
                                {
                                    try
                                    {
                                        // Try the "r" "S" and "U" formats, as well as RFC822 with 2 and 4 digit year
                                        var formats = new string[] { "r", "S", "U", "ddd, dd MMM yyyy HH:mm:ss zzzz", "ddd, dd MMM yy HH:mm:ss zzzz" };
                                        rssItem.PubDate = DateTime.ParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                                    }
                                    catch
                                    {
                                        // Everything failed.  Give up.
                                        rssItem.PubDate = config.Rss[thisRss].MostRecent;
                                    }
                                }

                                // If this item more recent than our latest pull, output it
                                if (rssItem.PubDate > config.Rss[thisRss].MostRecent)
                                {
                                    numFound++;
                                    if (numFound < 2 || config.Rss[thisRss].Type == "All")
                                    {
                                        SendPlainTextMessage(config.Rss[thisRss].Destination, rssItem.Title + "\r\n" + rssItem.Link);
                                    }
                                    newMostRecent = rssItem.PubDate.GetValueOrDefault();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Error reading Discord RSS " + config.Rss[thisRss].URL + ": " + ex.ToString());
                        }
                    }
                    else
                    {
                        // Parse Atom Format
                         try
                        {
                            var xDocument = XDocument.Load(config.Rss[thisRss].URL);
                            foreach (var xElement in xDocument.Root.Descendants().Where(i => i.Name.LocalName == "entry"))
                            {
                                // Parse xDocument to POCO
                                var atomEntry = new AtomEntry
                                {
                                    Title = xElement.Elements().First(i => i.Name.LocalName == "title").Value,
                                    Link = xElement.Elements().First(i => i.Name.LocalName == "link").Attributes().First(a => a.Name == "href").Value,
                                    Content = xElement.Elements().First(i => i.Name.LocalName == "content").Value,
                                    Updated = DateTime.Parse(xElement.Elements().First(i => i.Name.LocalName == "updated").Value)
                                };

                                // If this item more recent than our latest pull, update config
                                if (atomEntry.Updated > config.Rss[thisRss].MostRecent)
                                {
                                    numFound++;
                                    if (numFound < 2 || config.Rss[thisRss].Type == "All")
                                    {
                                        SendPlainTextMessage(config.Rss[thisRss].Destination, atomEntry.Title + "\r\n" + atomEntry.Link);
                                    }
                                    newMostRecent = atomEntry.Updated;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Error reading Discord Atom " + config.Rss[thisRss].URL + ": " + ex.ToString());
                        }
                    }

                    Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Found " +numFound + " items on Discord RSS " + config.Rss[thisRss].URL + "...");

                    // Write back to config if we found anything
                    if (newMostRecent > config.Rss[thisRss].MostRecent)
                    {
                        config.Rss[thisRss].MostRecent = newMostRecent;
                        writeConfig = true;
                    }
                }
            }

            // Save Config
            if (writeConfig)
            {
                await System.IO.File.WriteAllTextAsync("config.json", JsonConvert.SerializeObject(config, Formatting.Indented));
            }
        }

        private Task Log(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private Task MessageReceived(SocketMessage message)
        {
            Processor.TextMessage(this, config, message.Channel.Id.ToString(), message.Content, message.Author.Username, message.Author.Username);
            return Task.CompletedTask;
        }

        public override void SendFile(string destination, string Url, string filename = "", string referrer = "https://duckduckgo.com")
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + Url);
            SendStatusTyping(destination);
            var httpClient = new ProHttpClient()
            {
                ReferrerUri = referrer
            };
            var stream = httpClient.DownloadData(Url).Result;
            if (filename?.Length == 0)
            {
                filename = Url.Substring(Url.LastIndexOf("/", StringComparison.Ordinal) + 1, 9999);
            }
            var channel = bot.GetChannel(Convert.ToUInt64(destination)) as IMessageChannel;
            channel.SendFileAsync(stream, filename);
        }

        public override void SendImage(string destination, string Url, string caption, string referrer = "https://duckduckgo.com")
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + Url + " : " + caption);
            SendStatusTyping(destination);
            try
            {
                var httpClient = new ProHttpClient()
                {
                    ReferrerUri = referrer
                };
                var stream = httpClient.DownloadData(Url).Result;
                var channel = bot.GetChannel(Convert.ToUInt64(destination)) as IMessageChannel;
                var extension = ".jpg";
                if (Url.Contains(".gif") || Url.Contains("image/gif"))
                {
                    extension = ".gif";
                }
                else if (Url.Contains(".png") || Url.Contains("image/png"))
                {
                    extension = ".png";
                }
                else if (Url.Contains(".tif"))
                {
                    extension = ".tif";
                }
                else if (Url.Contains(".bmp"))
                {
                    extension = ".bmp";
                }
                SendStatusUploadingPhoto(destination);
                channel.SendFileAsync(stream, "image" + extension, caption);
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                Console.WriteLine("Unable to download " + ex.HResult + " " + ex.Message);
                SendPlainTextMessage(destination, Url);
            }
            catch (System.Net.WebException ex)
            {
                Console.WriteLine("Unable to download " + ex.HResult + " " + ex.Message);
                SendPlainTextMessage(destination, Url);
            }
            catch (Exception ex)
            {
                Console.WriteLine(Url + " Threw: " + ex.Message);
                SendPlainTextMessage(destination, "The Great & Powerful Trixie got bored while waiting for that to download.  Try later.  " + ex.Message);
            }
        }

        public override void SendHTMLMessage(string destination, string message)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + message);
            var channel = bot.GetChannel(Convert.ToUInt64(destination)) as IMessageChannel;
            channel.SendMessageAsync(message);
        }

        public override void SendLocation(string destination, float latitude, float longitude)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + latitude + " + " + longitude);
            var channel = bot.GetChannel(Convert.ToUInt64(destination)) as IMessageChannel;
            channel.SendMessageAsync("http://maps.google.com/maps?z=12&t=m&q=loc:" + latitude + "+" + longitude);
        }

        public override void SendMarkdownMessage(string destination, string message)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + message);
            var channel = bot.GetChannel(Convert.ToUInt64(destination)) as IMessageChannel;
            channel.SendMessageAsync(message);
        }

        public override void SendPlainTextMessage(string destination, string message)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + message);
            var channel = bot.GetChannel(Convert.ToUInt64(destination)) as IMessageChannel;
            channel.SendMessageAsync(message);
        }

        public override void SendStatusTyping(string destination)
        {
            var channel = bot.GetChannel(Convert.ToUInt64(destination)) as IMessageChannel;
            channel.TriggerTypingAsync();
        }

        public override void SendStatusUploadingFile(string destination)
        {
            var channel = bot.GetChannel(Convert.ToUInt64(destination)) as IMessageChannel;
            channel.TriggerTypingAsync();
        }

        public override void SendStatusUploadingPhoto(string destination)
        {
            var channel = bot.GetChannel(Convert.ToUInt64(destination)) as IMessageChannel;
            channel.TriggerTypingAsync();
        }
    }
}