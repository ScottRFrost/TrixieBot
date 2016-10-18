using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace TrixieBot
{
    public class DiscordProtocol : BaseProtocol
    {
        DiscordSocketClient bot;

        public DiscordProtocol(IConfigurationSection keys) : base(keys)
        {
            this.keys = keys;
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

            // Log in
            await bot.LoginAsync(TokenType.Bot, keys["DiscordToken"]);
            await bot.ConnectAsync();
            await Task.Delay(-1); // Just wait forever
            return true;
        }

        Task Log(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        Task MessageReceived(SocketMessage message)
        {
            Processor.TextMessage(this, keys, message.Channel.Id.ToString(), message.Content, message.Author.Username, message.Author.Username);
            return Task.CompletedTask;
        }

        public override void SendFile(string destination, string Url, string filename = "", string referrer = "https://duckduckgo.com")
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " " + destination + " > " + Url);
            SendStatusTyping(destination);
            var httpClient = new ProHttpClient();
            httpClient.ReferrerUri = referrer;
            var stream = httpClient.DownloadData(Url).Result;
            if (filename == "")
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
                var httpClient = new ProHttpClient();
                httpClient.ReferrerUri = referrer;
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