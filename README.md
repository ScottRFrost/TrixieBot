# TrixieBot
A simple multi-protocol chat bot for .NET Core 2.x

Telegram implemented using [Telegram.bot](https://github.com/MrRoundRobin/telegram.bot)
by [MrRoundRobin](https://github.com/MrRoundRobin)

Discord implemented using [Discord.Net](https://github.com/RogueException/Discord.Net) by [RogueException](https://github.com/RogueException)

Please take a look at BaseProtocol.  I tried to make it as easy as possible to add new protocols.  Implement a new one and send me a PR!

# To Build
Just do a dotnet publish, for example:

    dotnet publish -r win10-x64
or
    dotnet publish -r linux-x64