# TrixieBot
A simple multi-protocol chat bot for .NET 7.x

Telegram implemented using [Telegram.Bot](https://github.com/TelegramBots/telegram.bot)

Discord implemented using [Discord.Net](https://github.com/discord-net/Discord.Net)

Please take a look at BaseProtocol.  I tried to make it as easy as possible to add new protocols.  Implement a new one and send me a PR!

# To Build
Just do a dotnet publish, for example:

    dotnet publish -r win10-x64
or
    dotnet publish -r linux-x64