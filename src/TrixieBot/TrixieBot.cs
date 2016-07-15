﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


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
            var telegramResult = await telegram;
            return false;
        }
    }
}
