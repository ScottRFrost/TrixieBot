using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TrixieBot
{
    public class TelegramProtocol : BaseProtocol
    {
        private TelegramBotClient bot;
        private User me;

        public TelegramProtocol(Config config) : base(config)
        {
            this.config = config;
        }

        public override async Task<bool> Start()
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Telegram Protocol starting...");
            bot = new TelegramBotClient(config.Keys.TelegramKey);
            me = await bot.GetMeAsync().ConfigureAwait(false);
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + me.Username + " started at " + DateTime.Now);

            // Start RSS / Atom processing if there are any items for this protocol
            Timer timer;
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Checking for Telegram RSS...");
            if (config.Rss.Length > 0)
            {
                foreach (var rss in config.Rss)
                {
                    if (rss.Protocol.ToUpperInvariant() == "TELEGRAM")
                    {
                        Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " At least one Telegram RSS found starting timer...");
                        timer = new Timer(OnTimerTick, null, 120000, 900000);
                        break;
                    }
                }
            }

            var offset = 0;
            while (true)
            {
                var updates = new Update[0];
                try
                {
                    updates = await bot.GetUpdatesAsync(offset).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    // Don't care
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " ERROR WHILE GETTING UPDATES - " + ex);
                }
                foreach (var update in updates)
                {
                    offset = update.Id + 1;
                    try
                    {
                        switch (update.Type)
                        {
                            case UpdateType.MessageUpdate:
                                if (update.Message.Text != null)
                                {
                                    Processor.TextMessage(this, config, update.Message.Chat.Id.ToString(),
                                    update.Message.Text.Replace("@" + me.Username, ""),
                                    update.Message.From.Username, update.Message.From.FirstName + " " + update.Message.From.LastName,
                                    update.Message.ReplyToMessage == null ? "" : update.Message.ReplyToMessage.Text);
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR WHILE PROCESSING:\r\n" + ex);
                    }
                }
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        public async void OnTimerTick(object configObject)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Checking Telegram RSS / Atom Feeds...");

            // Read Config
            var config = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText("config.json"));

            // Parse each feed
            var writeConfig = false;
            for (var thisRss = 0; thisRss < config.Rss.Length; thisRss++)
            {
                if (config.Rss[thisRss].Protocol.ToUpperInvariant() == "TELEGRAM")
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
                                        // Try the "r" "S" and "U" formats, as well as RFC822
                                        var formats = new string[] { "r", "S", "U", "ddd, dd MMM yyyy HH:mm:ss zzz" };
                                        rssItem.PubDate = DateTime.ParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                                    }
                                    catch
                                    {
                                        // Everything failed.  Give up.
                                        rssItem.PubDate = DateTime.Now;
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
                            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Error reading Telegram RSS " + config.Rss[thisRss].URL + ": " + ex.ToString());
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
                            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Error reading Telegram Atom " + config.Rss[thisRss].URL + ": " + ex.ToString());
                        }
                    }

                    Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Found " + numFound + " items on Telegram RSS " + config.Rss[thisRss].URL + "...");

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
                filename = Url.Substring(Url.LastIndexOf("/") + 1, 9999);
            }
            var photo = new FileToSend(filename, stream);
            bot.SendDocumentAsync(destination, photo);
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
                var photo = new FileToSend("Photo" + extension, stream);
                SendStatusUploadingPhoto(destination);
                if (extension == ".gif")
                {
                    bot.SendDocumentAsync(destination, photo);
                }
                else
                {
                    bot.SendPhotoAsync(destination, photo, caption?.Length == 0 ? Url : caption);
                }
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
                SendPlainTextMessage(destination, "The Great & Powerful Trixie got bored while waiting for that to download.  Try later.  " +  ex.Message);
            }
        }

        public override void SendHTMLMessage(string destination, string message)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + message);
            
            bot.SendTextMessageAsync(destination, message, ParseMode.Html);
        }

        public override void SendLocation(string destination, float latitude, float longitude)
        {
            bot.SendLocationAsync(destination, latitude, longitude);
        }

        public override void SendMarkdownMessage(string destination, string message)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + message);
            bot.SendTextMessageAsync(destination, message, ParseMode.Markdown);
        }

        public override void SendPlainTextMessage(string destination, string message)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + message);
            bot.SendTextMessageAsync(destination, message);
        }

        public override void SendStatusTyping(string destination)
        {
            bot.SendChatActionAsync(destination, ChatAction.Typing);
        }

        public override void SendStatusUploadingFile(string destination)
        {
            bot.SendChatActionAsync(destination, ChatAction.UploadDocument);
        }

        public override void SendStatusUploadingPhoto(string destination)
        {
            bot.SendChatActionAsync(destination, ChatAction.UploadPhoto);
        }
    }
}
