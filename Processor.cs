using AngleSharp;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace TrixieBot
{
    public static class Processor
    {
        [RequiresUnreferencedCode("Enumeration on Dynamics")]
        public async static void TextMessage(BaseProtocol protocol, Config config, string replyDestination, string text, string replyUsername = "", string replyFullname = "", string replyMessage = "")
        {
            // Set up 
            var httpClient = new ProHttpClient();
            var stringBuilder = new StringBuilder();
            var angleSharpConfig = Configuration.Default.WithDefaultLoader();
            var epoch = Math.Floor((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();

            // Log to console
            Console.WriteLine(replyDestination + " < " + replyUsername + " - " + text);

            // Allow ! or /
            if (text.StartsWith("!", StringComparison.Ordinal))
            {
                text = "/" + text[1..];
            }

            // Parse
            string command;
            string body;
            if (text.StartsWith("/s/", StringComparison.Ordinal))
            {
                command = "/s"; // special case for sed
                body = text[2..];
            }
            else
            {
                command = text.Split(' ')[0];
                body = text.Replace(command, "").Trim();
            }
            var args = body.Split(' ');

            try
            {
                switch (command.ToLowerInvariant())
                {
                    case "/alahaakboom":
                    case "/alahuakboom":
                    case "/allahaakboom":
                    case "/allahuakboom":
                    case "/resettheclock":
                    case "/resetclock":
                    case "/jihad":
                        protocol.SendImage(replyDestination, "http://i.imgur.com/BA1dOl5.jpg", "Days since last Muslim terrorist attack"); // Sorry if you find this offensive, it was specifically requested by some users
                        break;

                    case "/cat":
                        protocol.SendImage(replyDestination, "http://thecatapi.com/api/images/get?format=src&type=jpg,png", "Cat");
                        break;

                    case "/crypties":
                        var crypties = new StringBuilder();
                        var btcUsdTask = httpClient.DownloadString("https://api.kraken.com/0/public/Ticker?pair=xbtusd");
                        var adaUsdTask = httpClient.DownloadString("https://api.kraken.com/0/public/Ticker?pair=adausd");
                        var adaXbtTask = httpClient.DownloadString("https://api.kraken.com/0/public/Ticker?pair=adaxbt");
                        var bnbUsdTask = httpClient.DownloadString("https://api.binance.us/api/v3/avgPrice?symbol=BNBUSD");
                        var bnbXbtTask = httpClient.DownloadString("https://api.binance.us/api/v3/avgPrice?symbol=BNBBTC");
                        var ethUsdTask = httpClient.DownloadString("https://api.kraken.com/0/public/Ticker?pair=ethusd");
                        var ethXbtTask = httpClient.DownloadString("https://api.kraken.com/0/public/Ticker?pair=ethxbt");
                        var scUsdTask = httpClient.DownloadString("https://api.kraken.com/0/public/Ticker?pair=scusd");
                        var scXbtTask = httpClient.DownloadString("https://api.kraken.com/0/public/Ticker?pair=scxbt");
                        var xlmUsdtask = httpClient.DownloadString("https://api.kraken.com/0/public/Ticker?pair=xlmusd");
                        var xlmXbttask = httpClient.DownloadString("https://api.kraken.com/0/public/Ticker?pair=xlmxbt");
                        var xmrUsdtask = httpClient.DownloadString("https://api.kraken.com/0/public/Ticker?pair=xmrusd");
                        var xmrXbtTask = httpClient.DownloadString("https://api.kraken.com/0/public/Ticker?pair=xmrxbt");

                        await Task.WhenAll(btcUsdTask, adaUsdTask, adaXbtTask, bnbUsdTask, bnbXbtTask, ethUsdTask, ethXbtTask, scUsdTask, scXbtTask, xlmUsdtask, xlmXbttask, xmrUsdtask, xmrXbtTask).ConfigureAwait(false);

                        dynamic btc = JObject.Parse(btcUsdTask.Result);
                        decimal btcUsd = btc?.result?.XXBTZUSD?.c[0] ?? 0M;
                        crypties.Append("BTC ").AppendLine(btcUsd.ToString("c2"));

                        dynamic ada = JObject.Parse(adaUsdTask.Result);
                        decimal adaUsd = ada?.result?.ADAUSD?.c[0] ?? 0M;
                        ada = JObject.Parse(adaXbtTask.Result);
                        decimal adaXbt = ada?.result?.ADAXBT?.c[0] ?? 0M;
                        crypties.Append("ADA ").Append(adaUsd.ToString("c5")).Append(" B").AppendLine(adaXbt.ToString("n8"));

                        dynamic bnb = JObject.Parse(bnbUsdTask.Result);
                        decimal bnbUsd = bnb?.price ?? 0M;
                        bnb = JObject.Parse(bnbXbtTask.Result);
                        decimal bnbXbt = bnb?.price ?? 0M;
                        crypties.Append("BNB ").Append(bnbUsd.ToString("c4")).Append(" B").AppendLine(bnbXbt.ToString("n8"));

                        dynamic eth = JObject.Parse(ethUsdTask.Result);
                        decimal ethUsd = eth?.result?.XETHZUSD?.c[0] ?? 0M;
                        eth = JObject.Parse(ethXbtTask.Result);
                        decimal ethXbt = eth?.result?.XETHXXBT?.c[0] ?? 0M;
                        crypties.Append("ETH ").Append(ethUsd.ToString("c2")).Append(" B").AppendLine(ethXbt.ToString("n6"));

                        dynamic sc = JObject.Parse(scUsdTask.Result);
                        decimal scUsd = sc?.result?.SCUSD?.c[0] ?? 0M;
                        sc = JObject.Parse(scXbtTask.Result);
                        decimal scXbt = sc?.result?.SCXBT?.c[0] ?? 0M;
                        crypties.Append("SC ").Append(scUsd.ToString("c5")).Append(" B").AppendLine(scXbt.ToString("n10"));

                        dynamic xlm = JObject.Parse(xlmUsdtask.Result);
                        decimal xlmUsd = xlm?.result?.XXLMZUSD?.c[0] ?? 0M;
                        xlm = JObject.Parse(xlmXbttask.Result);
                        decimal xlmXbt = xlm?.result?.XXLMXXBT?.c[0] ?? 0M;
                        crypties.Append("XLM ").Append(xlmUsd.ToString("c5")).Append(" B").AppendLine(xlmXbt.ToString("n8"));

                        dynamic xmr = JObject.Parse(xmrUsdtask.Result);
                        decimal xmrUsd = xmr?.result?.XXMRZUSD?.c[0] ?? 0M;
                        xmr = JObject.Parse(xmrXbtTask.Result);
                        decimal xmrXbt = xmr?.result?.XXMRXXBT?.c[0] ?? 0M;
                        crypties.Append("XMR ").Append(xmrUsd.ToString("c2")).Append(" B").AppendLine(xmrXbt.ToString("n6"));

                        protocol.SendPlainTextMessage(replyDestination, crypties.ToString());
                        break;

                    case "/doge":
                        protocol.SendImage(replyDestination, "http://dogr.io/wow/" + body.Replace(",", "/").Replace(" ", "") + ".png", "Wow");
                        break;

                    case "/echo":
                        protocol.SendPlainTextMessage(replyDestination, body);
                        break;

                    case "/forecast":
                        if (body.Length < 2)
                        {
                            body = "Cincinnati, OH";
                        }

                        protocol.SendStatusTyping(replyDestination);
                        dynamic dfor = JObject.Parse(httpClient.DownloadString("http://api.wunderground.com/api/" + config.Keys.WundergroundKey + "/forecast/q/" + body + ".json").Result);
                        if (dfor.forecast == null || dfor.forecast.txt_forecast == null)
                        {
                            protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.  Try \"City, ST\" or \"City, Country\" next time.");
                            break;
                        }
                        for (var ifor = 0; ifor < Enumerable.Count(dfor.forecast.txt_forecast.forecastday) - 1; ifor++)
                        {
                            stringBuilder.AppendLine(dfor.forecast.txt_forecast.forecastday[ifor].title.ToString() + ": " + dfor.forecast.txt_forecast.forecastday[ifor].fcttext.ToString());
                        }
                        protocol.SendPlainTextMessage(replyDestination, stringBuilder.ToString());
                        break;

                    case "/help":
                        protocol.SendPlainTextMessage(replyDestination, "The Great & powerful Trixie understands the following commands:\r\n" +
                            "/cat /doge /fat /forecast /help /image /imdb /google /joke /map /outside /overwatch /pony /radar /satellite /stock /stock7 /stockyear /translate /translateto /trixie /version /weather /wiki /wow /ww");
                        /* Send this string of text to BotFather to register the bot's commands:
    cat - Get a picture of a cat
    doge - Dogeify a comma sep list of terms
    fat - Nutrition information
    forecast - Weather forecast
    help - Displays help text
    image - Search for an image
    imdb - Search IMDB for a movie name
    google - Search Google
    map - Returns a location for the given search
    joke - Returns a random joke from /r/jokes on Reddit
    outside - Webcam image
    overwatch - Overwatch Stats
    pony - Ponies matching comma separated tags
    radar - Weather radar
    remind - Sets a reminder message after X minutes
    satellite - Weather Satellite
    stock - US Stock Chart (1 day)
    stock7 - US Stock Chart (7 day)
    stockyear - US Stock Chart (12 month)
    translate - Translate to english
    translateto - Translate to a given language
    trixie - Wolfram Alpha logic search
    version - Display version info
    weather - Current weather conditions
    wiki - Search Wikipedia
    wow - World of Warcraft character data
    ww - WeightWatcher PointsPlus calc
                        */
                        break;

                    case "/joke":
                        protocol.SendStatusTyping(replyDestination);
                        dynamic djoke = JObject.Parse(httpClient.DownloadString("https://api.reddit.com/r/jokes/top?t=day&limit=5").Result);
                        var rjoke = new Random();
                        var ijokemax = Enumerable.Count(djoke.data.children);
                        if (ijokemax > 4)
                        {
                            ijokemax = 4;
                        }
                        var ijoke = rjoke.Next(0, ijokemax);
                        protocol.SendPlainTextMessage(replyDestination, djoke.data.children[ijoke].data.title.ToString() + " " + djoke.data.children[ijoke].data.selftext.ToString());
                        break;

                    case "/pony":
                    case "/pone":
                        if (body.Length < 2)
                        {
                            protocol.SendPlainTextMessage(replyDestination, "I like ponies too.  What kind of pony would you like me to search for?");
                            break;
                        }
                        protocol.SendStatusTyping(replyDestination);
                        dynamic dpony = JObject.Parse(httpClient.DownloadString("https://derpibooru.org/search.json?q=safe," + Uri.EscapeDataString(body)).Result);
                        if (dpony.search == null)
                        {
                            protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.");
                            break;
                        }
                        var rpony = new Random();
                        var iponymax = Enumerable.Count(dpony.search);
                        if (iponymax < 1)
                        {
                            protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.");
                            break;
                        }
                        if (iponymax > 5)
                        {
                            iponymax = 5;
                        }
                        var ipony = rpony.Next(0, iponymax);
                        protocol.SendImage(replyDestination, "https:" + dpony.search[ipony].representations.large, "https:" + dpony.search[ipony].image);
                        break;

                    case "/radar":
                        if (body.Length < 2)
                        {
                            body = "Cincinnati, OH";
                        }
                        protocol.SendFile(replyDestination, "http://api.wunderground.com/api/" + config.Keys.WundergroundKey + "/animatedradar/q/" + body + ".gif?newmaps=1&num=15&width=1024&height=1024", "Radar.gif");
                        break;

                    case "/s":
                        if (body.Length < 2 || replyMessage?.Length == 0)
                        {
                            protocol.SendPlainTextMessage(replyDestination, "This must be done as a reply in the format /s/replace this/replace with/");
                        }
                        else
                        {
                            var sed = body.Split('/');
                            if (sed.Length != 4)
                            {
                                protocol.SendPlainTextMessage(replyDestination, "The only sed command parsed is /s/replace this/replace with/");
                            }
                            else
                            {
                                protocol.SendMarkdownMessage(replyDestination, "*" + replyFullname + "* \r\n" + replyMessage.Replace(sed[1], sed[2]));
                            }
                        }
                        break;

                    case "/stock":
                        body = body.ToUpperInvariant();
                        if (body.Length < 1 || body.Length > 5)
                        {
                            body = "TSLA";
                        }
                        protocol.SendStatusTyping(replyDestination);
                        protocol.SendImage(replyDestination, "https://app.quotemedia.com/quotetools/getChart?webmasterId=102684&snap=true&symbol=" + body + "&chscale=1d&chtype=AreaChart&chwid=1280&chhig=720&chfill=ffac6a&chfill2=febf8b&chln=fe9540&chmrg=0&chfrmon=false&chton=false&chbg=ffffff&chdon=false&chgrdon=true&chbdron=true&chbgch=ffffff&chcpy=ffffff&chtcol=ffffff", "Chart for " + body + " as of " + DateTime.Now.ToString("MM/dd/yyy HH:mm:ss"));
                        break;

                    case "/stock5":
                        body = body.ToUpperInvariant();
                        if (body.Length < 1 || body.Length > 5)
                        {
                            body = "TSLA";
                        }
                        protocol.SendStatusTyping(replyDestination);
                        protocol.SendImage(replyDestination, "https://app.quotemedia.com/quotetools/getChart?webmasterId=102684&snap=true&symbol=" + body + "&chscale=5d&chtype=AreaChart&chwid=1280&chhig=720&chfill=ffac6a&chfill2=febf8b&chln=fe9540&chmrg=0&chfrmon=false&chton=false&chbg=ffffff&chdon=false&chgrdon=true&chbdron=true&chbgch=ffffff&chcpy=ffffff&chtcol=ffffff", "Chart for " + body + " as of " + DateTime.Now.ToString("MM/dd/yyy HH:mm:ss"));
                        break;

                    case "/stockyear":
                        body = body.ToUpperInvariant();
                        if (body.Length < 1 || body.Length > 5)
                        {
                            body = "TSLA";
                        }
                        protocol.SendStatusTyping(replyDestination);
                        protocol.SendImage(replyDestination, "https://app.quotemedia.com/quotetools/getChart?webmasterId=102684&snap=true&symbol=" + body + "&chscale=1y&chtype=AreaChart&chwid=1280&chhig=720&chfill=ffac6a&chfill2=febf8b&chln=fe9540&chmrg=0&chfrmon=false&chton=false&chbg=ffffff&chdon=false&chgrdon=true&chbdron=true&chbgch=ffffff&chcpy=ffffff&chtcol=ffffff", "Chart for " + body + " as of " + DateTime.Now.ToString("MM/dd/yyy HH:mm:ss"));
                        break;

                    //case "/translateto":
                    //    if (body?.Length == 0)
                    //    {
                    //        protocol.SendPlainTextMessage(replyDestination, "Usage: /translateto <Language Code> <English Text>");
                    //        break;
                    //    }
                    //    protocol.SendStatusTyping(replyDestination);
                    //    var lang = body.Substring(0, body.IndexOf(" ", StringComparison.Ordinal));
                    //    var query = body.Substring(body.IndexOf(" ", StringComparison.Ordinal) + 1);
                    //    httpClient.AuthorizationHeader = "Basic " + config.Keys.BingKey;
                    //    dynamic dtto = JObject.Parse(httpClient.DownloadString("https://api.datamarket.azure.com/Bing/MicrosoftTranslator/v1/Translate?Text=%27" + Uri.EscapeDataString(query) + "%27&To=%27" + lang + "%27&$format=json").Result);
                    //    httpClient.AuthorizationHeader = string.Empty;
                    //    if (dtto.d == null || dtto.d.results == null || Enumerable.Count(dtto.d.results) < 1 || dtto.d.results[0].Text == null)
                    //    {
                    //        protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.  Try harder next time.");
                    //    }
                    //    else
                    //    {
                    //        protocol.SendPlainTextMessage(replyDestination, dtto.d.results[0].Text);
                    //    }
                    //    break;

                    //case "/translate":
                    //    if (body?.Length == 0)
                    //    {
                    //        protocol.SendPlainTextMessage(replyDestination, "Usage: /translate <Foreign Text>");
                    //        break;
                    //    }
                    //    protocol.SendStatusTyping(replyDestination);
                    //    httpClient.AuthorizationHeader = "Basic " + config.Keys.BingKey;
                    //    dynamic dtrans = JObject.Parse(httpClient.DownloadString("https://api.datamarket.azure.com/Bing/MicrosoftTranslator/v1/Translate?Text=%27" + Uri.EscapeDataString(body) + "%27&To=%27en%27&$format=json").Result);
                    //    httpClient.AuthorizationHeader = string.Empty;
                    //    if (dtrans.d == null || dtrans.d.results == null || Enumerable.Count(dtrans.d.results) < 1 || dtrans.d.results[0].Text == null)
                    //    {
                    //        protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.  Try harder next time.");
                    //    }
                    //    else
                    //    {
                    //        protocol.SendPlainTextMessage(replyDestination, dtrans.d.results[0].Text);
                    //    }
                    //    break;

                    case "/trixie":
                        if (body?.Length == 0)
                        {
                            protocol.SendPlainTextMessage(replyDestination, "Usage: /trixie <Query>");
                            break;
                        }
                        protocol.SendStatusTyping(replyDestination);
                        var xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(httpClient.DownloadString("http://api.wolframalpha.com/v2/query?input=" + Uri.EscapeDataString(body) + "&appid=" + config.Keys.WolframAppID).Result);
                        var queryResult = xmlDoc.SelectSingleNode("/queryresult");
                        if (queryResult == null || queryResult?.Attributes == null || queryResult.Attributes?["success"] == null || queryResult.Attributes?["success"].Value != "true")
                        {
                            protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.  Try harder next time.");
                            break;
                        }
                        var pods = queryResult.SelectNodes("pod");
                        foreach (var pod in pods.Cast<XmlNode>().Where(pod => pod.Attributes != null && pod.Attributes["title"].Value != "Input interpretation"))
                        {
                            // Parse Image
                            //if (replyImage == string.Empty)
                            //{
                            try
                            {
                                var subPodImage = pod.SelectSingleNode("subpod/img");
                                if (subPodImage.Attributes != null)
                                {
                                    protocol.SendImage(replyDestination, subPodImage.Attributes?["src"].Value.Trim(), pod.Attributes["title"].Value);
                                }
                            }
                            catch
                            {
                                // Don't care
                            }
                            //}

                            // Parse plain text
                            try
                            {
                                var subPodPlainText = pod.SelectSingleNode("subpod/plaintext");
                                if (subPodPlainText == null || subPodPlainText.InnerText.Trim().Length == 0) continue;
                                var podName = pod.Attributes?["title"].Value.Trim();
                                if (podName == "Response" || podName == "Result")
                                {
                                    stringBuilder.AppendLine(subPodPlainText.InnerText);
                                }
                                else
                                {
                                    stringBuilder.Append(podName).Append(": ").AppendLine(subPodPlainText.InnerText);
                                }
                            }
                            catch
                            {
                                // Don't care
                            }
                        }
                        protocol.SendPlainTextMessage(replyDestination, stringBuilder.ToString());
                        break;

                    case "/version":
                    case "/about":
                        protocol.SendPlainTextMessage(replyDestination, "Trixie Is Best Pony Bot\r\nRelease fourty-two for .NET 6.x\r\nBy http://scottrfrost.github.io");
                        break;

                    case "/wiki":
                        if (body?.Length == 0)
                        {
                            protocol.SendPlainTextMessage(replyDestination, "Usage: /wiki <Query>");
                            break;
                        }
                        protocol.SendStatusTyping(replyDestination);
                        var dwiki = JObject.Parse(httpClient.DownloadString("https://en.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exintro=&explaintext=&redirects=true&titles=" + Uri.EscapeDataString(body)).Result);
                        if (dwiki["query"].HasValues && dwiki["query"]["pages"].HasValues)
                        {
                            var page = dwiki["query"]["pages"].First().First();
                            if (Convert.ToString(page["pageid"]).Length > 0)
                            {
                                protocol.SendMarkdownMessage(replyDestination, "*" + page["title"] + "*\r\n" + page["extract"] + "\r\nhttps://en.wikipedia.org/?curid=" + page["pageid"]);
                            }
                            else
                            {
                                protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.");
                            }
                        }
                        else
                        {
                            protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.");
                        }
                        break;
                }
            }
            catch (System.Net.Http.HttpRequestException exception)
            {
                protocol.SendPlainTextMessage(replyDestination, exception.Message);
            }
            catch (Exception exception)
            {
                if (exception.InnerException?.Source == "System.Net.Http")
                {
                    protocol.SendPlainTextMessage(replyDestination, exception.InnerException.Message);
                }
                else
                {
                    protocol.SendPlainTextMessage(replyDestination, exception.ToString());
                }
            }
        }
    }
}
