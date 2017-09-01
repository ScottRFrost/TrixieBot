using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TrixieBot
{
    public class TelegramProtocol : BaseProtocol
    {
        private TelegramBotClient bot;
        private User me;

        public TelegramProtocol(IConfigurationSection keys) : base(keys)
        {
            this.keys = keys;
        }

        public override async Task<bool> Start()
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Telegram Protocol starting...");
            bot = new TelegramBotClient(keys["TelegramKey"]);
            me = await bot.GetMeAsync().ConfigureAwait(false);
            await bot.LeaveChatAsync(-166628).ConfigureAwait(false);
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + me.Username + " started at " + DateTime.Now);

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
                                    Processor.TextMessage(this, keys, update.Message.Chat.Id.ToString(),
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
