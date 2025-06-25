using System;
using System.Threading.Tasks;
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
            me = await bot.GetMe().ConfigureAwait(false);
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + me.Username + " started at " + DateTime.Now);

            // Start RSS / Atom processing if there are any items for this protocol
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Checking for Telegram RSS...");

            var offset = 0;
            //await bot.LeaveChatAsync(1234).ConfigureAwait(false); // Example
            while (true)
            {
                var updates = Array.Empty<Update>();
                try
                {
                    updates = await bot.GetUpdates(offset).ConfigureAwait(false);
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
                            case UpdateType.Message:
                                if (update.Message.Text != null)
                                {
                                    await Processor.TextMessage(this, config, update.Message.Chat.Id.ToString(),
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

        public override async void SendFile(string destination, string Url, string filename = "", string referrer = "https://duckduckgo.com")
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + Url);
            SendStatusTyping(destination);
            var httpClient = new ProHttpClient()
            {
                ReferrerUri = referrer
            };
            using var stream = httpClient.DownloadData(Url).Result;
            var inputFile = InputFile.FromStream(stream);
            if (filename?.Length == 0)
            {
                filename = Url.Substring(Url.LastIndexOf('/') + 1, 9999);
            }
            await bot.SendDocument(destination, inputFile, caption: filename);
        }

        public override async void SendImage(string destination, string Url, string caption, string referrer = "https://duckduckgo.com")
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + Url + " : " + caption);
            SendStatusTyping(destination);
            try
            {
                var httpClient = new ProHttpClient()
                {
                    ReferrerUri = referrer
                };
                await using var stream = await httpClient.DownloadData(Url);
                var inputFile = InputFile.FromStream(stream);
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
                if (extension == ".gif")
                {
                    await bot.SendDocument(destination, inputFile);
                }
                else
                {
                    await bot.SendPhoto(destination, inputFile, caption: caption?.Length == 0 ? Url : caption);
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

        public override async void SendHTMLMessage(string destination, string message)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + message);

            await bot.SendMessage(destination, message, parseMode: ParseMode.Html);
        }

        public override async void SendLocation(string destination, float latitude, float longitude)
        {
            await bot.SendLocation(destination, latitude, longitude);
        }

        public override async void SendMarkdownMessage(string destination, string message)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + message);
            await bot.SendMessage(destination, message, parseMode: ParseMode.Markdown);
        }

        public override async void SendPlainTextMessage(string destination, string message)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + message);
            await bot.SendMessage(destination, message);
        }

        public override async void SendStatusTyping(string destination)
        {
            await bot.SendChatAction(destination, ChatAction.Typing);
        }

        public override async void SendStatusUploadingFile(string destination)
        {
            await bot.SendChatAction(destination, ChatAction.UploadDocument);
        }

        public override async void SendStatusUploadingPhoto(string destination)
        {
            await bot.SendChatAction(destination, ChatAction.UploadPhoto);
        }
    }
}
