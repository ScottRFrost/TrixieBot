using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace TrixieBot
{
    public class TrixieBot
    {
        private IConfigurationSection keys;

        public TrixieBot()
        {
            // Read Config
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("config.json");
            var configRoot = configurationBuilder.Build();
            keys = configRoot.GetSection("Keys");
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Configuation read");
        }

        public async Task<bool> Start()
        {
            // Read Configuration
            var telegramKey = keys["TelegramKey"];

            // Start Telegram
            Task<bool> telegram;
            if (telegramKey != string.Empty)
            {
                var telegramProtocol = new TelegramProtocol(keys);
                telegram = telegramProtocol.Start();
            }
            else
            {
                telegram = Task.FromResult(false);
            }

            // Wait for all tasks to end
            var taskResult = Task.WaitAny(telegram);
            return false;
        }
    }
}
