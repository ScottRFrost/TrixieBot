using AngleSharp;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace TrixieBot
{
    public static class Processor
    {
        public static void TextMessage(BaseProtocol protocol, IConfigurationSection keys, string replyDestination, string text, string replyUsername = "", string replyFullname = "", string replyMessage = "")
        {
            // Read Configuration Keys
            var wundergroundKey = keys["WundergroundKey"];
            var bingKey = keys["BingKey"];
            var wolframAppId = keys["WolframAppID"];
            var httpClient = new ProHttpClient();
            var stringBuilder = new StringBuilder();

            // Log to console
            Console.WriteLine(replyDestination + " < " + replyUsername + " - " + text);

            // Allow ! or /
            if (text.StartsWith("!", StringComparison.Ordinal))
            {
                text = "/" + text.Substring(1);
            }

            // Parse
            string command;
            string body;
            if (text.StartsWith("/s/", StringComparison.Ordinal))
            {
                command = "/s"; // special case for sed
                body = text.Substring(2);
            }
            else
            {
                command = text.Split(' ')[0];
                body = text.Replace(command, "").Trim();
            }

            switch (command.ToLowerInvariant())
            {
                case "/beer":
                    if (body == string.Empty)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Usage: /beer <Name of beer>");
                        break;
                    }

                    protocol.SendStatusTyping(replyDestination);
                    var beerSearch = httpClient.DownloadString("http://www.beeradvocate.com/search/?q=" + Uri.EscapeDataString(body) + "&qt=beer").Result.Replace("\r", "").Replace("\n", "");

                    // Load First Result
                    var firstBeer = Regex.Match(beerSearch, @"<div id=""ba-content"">.*?<ul>.*?<li>.*?<a href=""(.*?)"">").Groups[1].Value.Trim();
                    if (firstBeer == string.Empty)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "The Great & Powerful Trixie was unable to find a beer name matching: " + body);
                        break;
                    }
                    var beer = httpClient.DownloadString("http://www.beeradvocate.com" + firstBeer).Result.Replace("\r", "").Replace("\n", "");
                    var beerName = Regex.Match(beer, @"<title>(.*?)</title>").Groups[1].Value.Replace(" | BeerAdvocate", string.Empty).Trim();
                    beer = Regex.Match(beer, @"<div id=""ba-content"">.*?<div>(.*?)<div style=""clear:both;"">").Groups[1].Value.Trim();
                    protocol.SendImage(replyDestination, Regex.Match(beer, @"img src=""(.*?)""").Groups[1].Value.Trim(), "http://www.beeradvocate.com" + firstBeer);
                    var beerScore = Regex.Match(beer, @"<span class=""BAscore_big ba-score"">(.*?)</span>").Groups[1].Value.Trim();
                    var beerScoreText = Regex.Match(beer, @"<span class=""ba-score_text"">(.*?)</span>").Groups[1].Value.Trim();
                    var beerbroScore = Regex.Match(beer, @"<span class=""BAscore_big ba-bro_score"">(.*?)</span>").Groups[1].Value.Trim();
                    var beerbroScoreText = Regex.Match(beer, @"<b class=""ba-bro_text"">(.*?)</b>").Groups[1].Value.Trim();
                    var beerHads = Regex.Match(beer, @"<span class=""ba-ratings"">(.*?)</span>").Groups[1].Value.Trim();
                    var beerAvg = Regex.Match(beer, @"<span class=""ba-ravg"">(.*?)</span>").Groups[1].Value.Trim();
                    var beerStyle = Regex.Match(beer, @"<b>Style:</b>.*?<b>(.*?)</b>").Groups[1].Value.Trim();
                    var beerAbv = beer.Substring(beer.IndexOf("(ABV):", StringComparison.Ordinal) + 10, 7).Trim();
                    var beerDescription = Regex.Match(beer, @"<b>Notes / Commercial Description:</b>(.*?)</div>").Groups[1].Value.Replace("|", "").Trim();
                    stringBuilder.Append(beerName.Replace("|", "- " + beerStyle + " by") + "\r\nScore: " + beerScore + " (" + beerScoreText + ") | Bros: " + beerbroScore + " (" + beerbroScoreText + ") | Avg: " + beerAvg + " (" + beerHads + " hads)\r\nABV: " + beerAbv + " | ");
                    stringBuilder.Append(beerDescription.Replace("<br>", " ").Trim());
                    protocol.SendPlainTextMessage(replyDestination, stringBuilder.ToString());
                    break;

                case "/cat":
                    protocol.SendImage(replyDestination, "http://thecatapi.com/api/images/get?format=src&type=jpg,png", "Cat");
                    break;

                case "/doge":
                    protocol.SendImage(replyDestination, "http://dogr.io/wow/" + body.Replace(",", "/").Replace(" ", "") + ".png", "Wow");
                    break;

                case "/echo":
                    protocol.SendPlainTextMessage(replyDestination, body);
                    break;

                case "/fat":
                    if (body == string.Empty)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Usage: /fat <Name of food>");
                        break;
                    }

                    protocol.SendStatusTyping(replyDestination);
                    var search = httpClient.DownloadString("http://www.calorieking.com/foods/search.php?keywords=" + body).Result.Replace("\r", "").Replace("\n", "");

                    // Load First Result
                    var firstUrl = Regex.Match(search, @"<a class=""food-search-result-name"" href=""([\w:\/\-\._]*)""").Groups[1].Value.Trim();
                    if (firstUrl == string.Empty)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "The Great & Powerful Trixie was unable to find a food name matching: " + body);
                        break;
                    }
                    var food = httpClient.DownloadString(firstUrl).Result.Replace("\r", "").Replace("\n", "");

                    // Scrape it
                    var label = string.Empty;
                    var protein = 0.0;
                    var carbs = 0.0;
                    var fat = 0.0;
                    var fiber = 0.0;
                    stringBuilder.Append(Regex.Match(food, @"<title>(.*)\ \|.*<\/title>").Groups[1].Value.Replace("Calories in ", "").Trim() + " per "); // Name of item
                    stringBuilder.Append(Regex.Match(food, @"<select name=""units"".*?<option.*?>(.*?)<\/option>", RegexOptions.IgnoreCase).Groups[1].Value.Trim() + "\r\n"); // Unit
                    foreach (Match fact in Regex.Matches(food, @"<td class=""(calories|label|amount)"">([a-zA-Z0-9\ &;<>=\/\""\.]*)<\/td>"))
                    {
                        switch (fact.Groups[1].Value.Trim().ToLowerInvariant())
                        {
                            case "calories":
                                stringBuilder.Append("Calories: " + fact.Groups[2].Value.Replace("Calories&nbsp;<span class=\"amount\">", "").Replace("</span>", "") + ", ");
                                break;

                            case "label":
                                label = fact.Groups[2].Value.Trim();
                                break;

                            case "amount":
                                stringBuilder.Append(label + ": " + fact.Groups[2].Value + ", ");
                                switch (label.ToLowerInvariant())
                                {
                                    case "protein":
                                        protein = Convert.ToDouble(fact.Groups[2].Value.Replace("mg", "").Replace("g", "").Replace("&lt;", "").Replace("&gt;", ""));
                                        break;

                                    case "total carbs.":
                                        carbs = Convert.ToDouble(fact.Groups[2].Value.Replace("mg", "").Replace("g", "").Replace("&lt;", "").Replace("&gt;", ""));
                                        break;

                                    case "total fat":
                                        fat = Convert.ToDouble(fact.Groups[2].Value.Replace("mg", "").Replace("g", "").Replace("&lt;", "").Replace("&gt;", ""));
                                        break;

                                    case "dietary fiber":
                                        fiber = Convert.ToDouble(fact.Groups[2].Value.Replace("mg", "").Replace("g", "").Replace("&lt;", "").Replace("&gt;", ""));
                                        break;
                                }
                                break;
                        }
                    }

                    // WW Points = (Protein/10.9375) + (Carbs/9.2105) + (Fat/3.8889) - (Fiber/12.5)
                    stringBuilder.Append("WW PointsPlus: " + Math.Round((protein / 10.9375) + (carbs / 9.2105) + (fat / 3.8889) - (fiber / 12.5), 1));

                    protocol.SendPlainTextMessage(replyDestination, stringBuilder.ToString());
                    break;

                case "/forecast":
                    if (body.Length < 2)
                    {
                        body = "Cincinnati, OH";
                    }

                    protocol.SendStatusTyping(replyDestination);
                    dynamic dfor = JObject.Parse(httpClient.DownloadString("http://api.wunderground.com/api/" + wundergroundKey + "/forecast/q/" + body + ".json").Result);
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
                        "/cat /doge /fat /forecast /help /image /imdb /google /joke /map /outside /overwatch /pony /radar /satellite /stock /stock7 /stockyear /translate /translateto /trixie /version /weather /wiki /ww");
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
ww - WeightWatcher PointsPlus calc
                    */
                    break;

                case "/image":
                case "/img":
                    if (body == string.Empty)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Usage: /image <Description of image to find>");
                        break;
                    }
                    protocol.SendStatusTyping(replyDestination);
                    httpClient.AuthorizationHeader = "Basic " + bingKey;
                    dynamic dimg = JObject.Parse(httpClient.DownloadString("https://api.datamarket.azure.com/Data.ashx/Bing/Search/Image?Market=%27en-US%27&Adult=%27Moderate%27&Query=%27" + Uri.EscapeDataString(body) + "%27&$format=json&$top=3").Result);
                    httpClient.AuthorizationHeader = string.Empty;
                    if (dimg.d == null || dimg.d.results == null || Enumerable.Count(dimg.d.results) < 1)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.  Try harder next time.");
                        break;
                    }
                    var rimg = new Random();
                    var iimgmax = Enumerable.Count(dimg.d.results);
                    if (iimgmax > 3)
                    {
                        iimgmax = 3;
                    }
                    var iimg = rimg.Next(0, iimgmax);
                    string imageUrl = dimg.d.results[iimg].MediaUrl.ToString();
                    protocol.SendImage(replyDestination, imageUrl, imageUrl);
                    break;

                case "/imdb":
                case "/rt":
                case "/movie":
                    if (body == string.Empty)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Usage: /imdb <Movie Title>");
                        break;
                    }
                    protocol.SendStatusTyping(replyDestination);

                    // Search Bing
                    httpClient.AuthorizationHeader = "Basic " + bingKey;
                    dynamic dimdb = JObject.Parse(httpClient.DownloadString("https://api.datamarket.azure.com/Data.ashx/Bing/Search/Web?Market=%27en-US%27&Adult=%27Moderate%27&Query=%27site%3Aimdb.com%20" + Uri.EscapeDataString(body) + "%27&$format=json&$top=1").Result);
                    httpClient.AuthorizationHeader = string.Empty;
                    if (dimdb.d == null || dimdb.d.results == null ||
                        Enumerable.Count(dimdb.d.results) < 1)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Trixie was unable to find a movie name matching:" + body);
                        break;
                    }

                    // Find correct /combined URL
                    string imdbUrl = dimdb.d.results[0].Url;
                    imdbUrl = (imdbUrl.Replace("/business", "").Replace("/combined", "").Replace("/faq", "").Replace("/goofs", "").Replace("/news", "").Replace("/parentalguide", "").Replace("/quotes", "").Replace("/ratings", "").Replace("/synopsis", "").Replace("/trivia", "") + "/combined").Replace("//combined", "/combined");

                    // Scrape it
                    var imdb = httpClient.DownloadString(imdbUrl).Result.Replace("\r", "").Replace("\n", "");
                    var title = Regex.Match(imdb, @"<title>(IMDb \- )*(.*?) \(.*?</title>", RegexOptions.IgnoreCase).Groups[2].Value.Trim();
                    var year = Regex.Match(imdb, @"<title>.*?\(.*?(\d{4}).*?\).*?</title>", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                    var rating = Regex.Match(imdb, @"<b>(\d.\d)/10</b>", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                    var votes = Regex.Match(imdb, @">(\d+,?\d*) votes<", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                    var plot = Regex.Match(imdb, @"Plot:</h5>.*?<div class=""info-content"">(.*?)(<a|</div)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                    var tagline = Regex.Match(imdb, @"Tagline:</h5>.*?<div class=""info-content"">(.*?)(<a|</div)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                    var poster = Regex.Match(imdb, @"<div class=""photo"">.*?<a name=""poster"".*?><img.*?src=""(.*?)"".*?</div>", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                    var posterFull = string.Empty;
                    if (!string.IsNullOrEmpty(poster) && poster.IndexOf("media-imdb.com", StringComparison.Ordinal) > 0)
                    {
                        poster = Regex.Replace(poster, @"_V1.*?.jpg", "_V1._SY200.jpg");
                        posterFull = Regex.Replace(poster, @"_V1.*?.jpg", "_V1._SX1280_SY1280.jpg");
                    }
                    if (title.Length < 2)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Trixie was unable to find a movie name matching: " + body);
                    }
                    else
                    {
                        // Try for RT score scrape
                        httpClient.AuthorizationHeader = "Basic " + bingKey;
                        dynamic drt = JObject.Parse(httpClient.DownloadString("https://api.datamarket.azure.com/Data.ashx/Bing/Search/Web?Market=%27en-US%27&Adult=%27Moderate%27&Query=%27site%3Arottentomatoes.com%20" + Uri.EscapeDataString(body) + "%27&$format=json&$top=1").Result);
                        httpClient.AuthorizationHeader = string.Empty;
                        if (drt.d != null && drt.d.results != null && Enumerable.Count(drt.d.results) > 0)
                        {
                            string rtUrl = drt.d.results[0].Url;
                            var rt = httpClient.DownloadString(rtUrl).Result;
                            //var rtCritic = Regex.Match(rt, @"<span class=""meter-value .*?<span>(.*?)</span>", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                            var rtCritic = Regex.Match(rt, @"<span class=""meter-value superPageFontColor""><span>(.*?)</span>", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                            var rtAudience = Regex.Match(rt, @"<span class=""superPageFontColor"" style=""vertical-align:top"">(.*?)</span>", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                            protocol.SendPlainTextMessage(replyDestination, title + " (" + year + ") - " + tagline + "\r\nIMDb: " + rating + " (" + votes + " votes) | RT critic: " + rtCritic + "% | RT audience: " + rtAudience + "\r\n" + plot);

                        }
                        else
                        {
                            var rt = httpClient.DownloadString("http://www.rottentomatoes.com/search/?search=" + Uri.EscapeDataString(body)).Result;
                            //var rtCritic = Regex.Match(rt, @"<span class=""meter-value .*?<span>(.*?)</span>", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                            var rtCritic = Regex.Match(rt, @"<span class=""meter-value superPageFontColor""><span>(.*?)</span>", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                            var rtAudience = Regex.Match(rt, @"<span class=""superPageFontColor"" style=""vertical-align:top"">(.*?)</span>", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                            protocol.SendPlainTextMessage(replyDestination, title + " (" + year + ") - " + tagline + "\r\nIMDb: " + rating + " (" + votes + " votes) | RT critic: " + rtCritic + "% | RT audience: " + rtAudience + "\r\n" + plot);
                        }

                        // Remove trailing pipe that sometimes occurs
                        //if (replyText.EndsWith("|"))
                        //{
                        //    protocol.SendPlainTextMessage(replyDestination, replyText.Substring(0, replyText.Length - 2).Trim();
                        //}

                        // Set referrer URI to grab IMDB poster
                        protocol.SendImage(replyDestination, posterFull, imdbUrl, imdbUrl);
                    }
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

                case "/map":
                case "/location":
                    if (body == string.Empty)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Usage: /map <Search Text>");
                        break;
                    }
                    protocol.SendStatusTyping(replyDestination);
                    dynamic dmap = JObject.Parse(httpClient.DownloadString("http://maps.googleapis.com/maps/api/geocode/json?address=" + Uri.EscapeDataString(body)).Result);
                    if (dmap == null || dmap.results == null || Enumerable.Count(dmap.results) < 1)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.  Try harder next time.");
                    }
                    else
                    {
                        protocol.SendLocation(replyDestination, (float)dmap.results[0].geometry.location.lat, (float)dmap.results[0].geometry.location.lng);
                    }
                    break;

                case "/google":
                case "/bing":
                    if (body == string.Empty)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Usage: /google <Search Text>");
                        break;
                    }
                    protocol.SendStatusTyping(replyDestination);
                    httpClient.AuthorizationHeader = "Basic " + bingKey;
                    dynamic dgoog = JObject.Parse(httpClient.DownloadString("https://api.datamarket.azure.com/Data.ashx/Bing/Search/Web?Market=%27en-US%27&Adult=%27Moderate%27&Query=%27" + Uri.EscapeDataString(body) + "%27&$format=json&$top=1").Result);
                    httpClient.AuthorizationHeader = string.Empty;
                    if (dgoog.d == null || dgoog.d.results == null || Enumerable.Count(dgoog.d.results) < 1)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.  Try harder next time.");
                    }
                    else
                    {
                        var rgoog = new Random();
                        var igoog = rgoog.Next(0, Enumerable.Count(dgoog.d.results));
                        string searchResult = dgoog.d.results[igoog].Title.ToString() + " | " + dgoog.d.results[igoog].Description.ToString() + "<br/>" + dgoog.d.results[igoog].Url;
                        protocol.SendHTMLMessage(replyDestination, searchResult);
                    }
                    break;

                case "/outside":
                    if (body.Length < 2)
                    {
                        body = "Cincinnati, OH";
                    }
                    protocol.SendStatusTyping(replyDestination);
                    dynamic dout = JObject.Parse(httpClient.DownloadString("http://api.wunderground.com/api/" + wundergroundKey + "/webcams/q/" + body + ".json").Result);
                    if (dout.webcams == null)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.  Try \"City, ST\" or \"City, Country\" next time.");
                        break;
                    }
                    var rout = new Random();
                    var iout = rout.Next(0, Enumerable.Count(dout.webcams));
                    protocol.SendImage(replyDestination, (string)dout.webcams[iout].CURRENTIMAGEURL, 
                    (string)dout.webcams[iout].organization + " " + (string)dout.webcams[iout].neighborhood + " " + (string)dout.webcams[iout].city + ", " + (string)dout.webcams[iout].state + "\r\n" + (string)dout.webcams[iout].CAMURL);
                    break;

                case "/overwatch":
                    if (body.Length < 2)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Usage: /overwatch <Battletag with no spaces>  eg: /overwatch SniperFox#1513");
                        break;
                    }
                    protocol.SendStatusTyping(replyDestination);

                    // Scrape it using AngleSharp
                    var angleSharpConfig = Configuration.Default.WithDefaultLoader();
                    var doc = BrowsingContext.New(angleSharpConfig).OpenAsync("https://playoverwatch.com/en-us/career/pc/us/" + body.Replace("#", "-")).Result;
                    string playerLevel = "0";
                    ushort parsedPlayerLevel = 0;
                    string competitiveRank = "0";
                    ushort parsedCompetitiveRank = 0;
                    var casual = new Dictionary<string, string>();
                    var competitive = new Dictionary<string, string>();
                    if (ushort.TryParse(doc.QuerySelector("div.player-level div")?.TextContent, out parsedPlayerLevel))
                        playerLevel = parsedPlayerLevel.ToString();
                    if (ushort.TryParse(doc.QuerySelector("div.competitive-rank div")?.TextContent, out parsedCompetitiveRank))
                        competitiveRank = parsedCompetitiveRank.ToString();
                    foreach (var elem in doc.QuerySelector("#quick-play div[data-category-id='0x02E00000FFFFFFFF']").QuerySelectorAll("table.data-table tbody tr"))
                    {
                        if (casual.ContainsKey(elem.Children[0].TextContent))
                            continue;
                        casual.Add(elem.Children[0].TextContent, elem.Children[1].TextContent);
                    }
                    foreach (var elem in doc.QuerySelector("#competitive-play div[data-category-id='0x02E00000FFFFFFFF']").QuerySelectorAll("table.data-table tbody tr"))
                    {
                        if (competitive.ContainsKey(elem.Children[0].TextContent))
                            continue;
                        competitive.Add(elem.Children[0].TextContent, elem.Children[1].TextContent);
                    }
                    if (competitive.Count > 0)
                    {
                        decimal wins = Convert.ToDecimal(competitive["Games Won"].Replace(",", ""));
                        decimal played = Convert.ToDecimal(competitive["Games Played"].Replace(",", ""));
                        decimal elims = Convert.ToDecimal(competitive["Eliminations"].Replace(",",""));
                        decimal assists = Convert.ToDecimal(competitive["Defensive Assists"].Replace(",", "")) + Convert.ToDecimal(competitive["Offensive Assists"].Replace(",", ""));
                        decimal deaths = Convert.ToDecimal(competitive["Deaths"].Replace(",", ""));
                        stringBuilder.AppendLine("*Competitive Play - Rank " + competitiveRank + " *");
                        stringBuilder.AppendLine(wins + " W, " + (played - wins).ToString() + " L (" + Math.Round((wins / played) * 100, 2) + "%) in " + competitive["Time Played"] + " played");
                        stringBuilder.AppendLine(elims + " Elims, " + assists + " Assists, " + deaths + " Deaths (" + Math.Round(((elims + assists) / deaths), 2) + " KDA)");
                        stringBuilder.AppendLine(competitive["Cards"] + " Cards, " + competitive["Medals - Gold"] + " Gold, " + competitive["Medals - Silver"] + " Silver, " + competitive["Medals - Bronze"] + " Bronze\r\n");

                    }
                    decimal cwins = Convert.ToDecimal(casual["Games Won"].Replace(",", ""));
                    decimal cplayed = Convert.ToDecimal(casual["Games Played"].Replace(",", ""));
                    decimal celims = Convert.ToDecimal(casual["Eliminations"].Replace(",", ""));
                    decimal cassists = Convert.ToDecimal(competitive["Defensive Assists"].Replace(",", "")) + Convert.ToDecimal(competitive["Offensive Assists"].Replace(",", ""));
                    decimal cdeaths = Convert.ToDecimal(casual["Deaths"].Replace(",", ""));
                    stringBuilder.AppendLine("*Filthy Casual Quick Play - Level " + playerLevel + " *");
                    stringBuilder.AppendLine(cwins + " W, " + (cplayed - cwins).ToString() + " L (" + Math.Round((cwins / cplayed) * 100, 2) + "%) in " + casual["Time Played"] + " played");
                    stringBuilder.AppendLine(celims + " Elims, " + cassists + " Assists, " + cdeaths + " Deaths (" + Math.Round(((celims + cassists) / cdeaths), 2) + " KDA)");
                    stringBuilder.AppendLine(casual["Cards"] + " Cards, " + casual["Medals - Gold"] + " Gold, " + casual["Medals - Silver"] + " Silver, " + casual["Medals - Bronze"] + " Bronze");
                    stringBuilder.AppendLine("https://playoverwatch.com/en-us/career/pc/us/" + body.Replace("#", "-"));
                    protocol.SendMakdownMessage(replyDestination, stringBuilder.ToString());
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
                    protocol.SendFile(replyDestination, "http://api.wunderground.com/api/" + wundergroundKey + "/animatedradar/q/" + body + ".gif?newmaps=1&num=15&width=1024&height=1024", "Radar.gif");
                    break;

                //case "/remind":
                //case "/remindme":
                //case "/reminder":
                //    if (body.Length < 2 || !body.Contains(" "))
                //    {
                //        protocol.SendPlainTextMessage(replyDestination, "Usage: /remind <minutes> <Reminder Text>";
                //    }
                //    else
                //    {
                //        var delayMinutesString = body.Substring(0, body.IndexOf(" ", StringComparison.Ordinal));
                //        int delayMinutes;
                //        if (int.TryParse(delayMinutesString, out delayMinutes))
                //        {
                //            if (delayMinutes > 1440 || delayMinutes < 1)
                //            {
                //                protocol.SendPlainTextMessage(replyDestination, "Reminders can not be set for longer than 1440 minutes (24 hours).";
                //            }
                //            else
                //            {
                //                DelayedMessage(bot, update.Message.Chat.Id, "@" + update.Message.From.Username + " Reminder: " + body.Substring(delayMinutesString.Length).Trim(), delayMinutes);
                //                protocol.SendPlainTextMessage(replyDestination, "OK, I'll remind you at " + DateTime.Now.AddMinutes(delayMinutes).ToString("MM/dd/yyyy HH:mm") + " (US Eastern)";
                //            }
                //        }
                //        else
                //        {
                //            protocol.SendPlainTextMessage(replyDestination, "Usage: /remind <minutes as positive integer> <Reminder Text>";
                //        }
                //    }
                //    break;

                case "/s":
                    if (body.Length < 2 || replyMessage == "")
                    {
                        protocol.SendPlainTextMessage(replyDestination, "This must be done as a reply in the format /s/replace this/replace with/");
                    }
                    else
                    {
                        var sed = body.Split('/');
                        if (sed.Length != 4)
                            protocol.SendPlainTextMessage(replyDestination, "The only sed command parsed is /s/replace this/replace with/");
                        else
                        {
                            protocol.SendMakdownMessage(replyDestination, "*" + replyFullname + "* \r\n" + replyMessage.Replace(sed[1], sed[2]));
                        }
                    }
                    break;

                case "/satellite":
                    if (body.Length < 2)
                    {
                        body = "Cincinnati, OH";
                    }
                    protocol.SendStatusTyping(replyDestination);
                    dynamic rsat = JObject.Parse(httpClient.DownloadString("http://api.wunderground.com/api/" + wundergroundKey + "/satellite/q/" + body + ".json").Result);
                    if (rsat.satellite == null || rsat.satellite.image_url == null)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.  Try \"City, ST\" or \"City, Country\" next time.");
                    }
                    else
                    {
                        string saturl = rsat.satellite.image_url;
                        protocol.SendImage(replyDestination, saturl.Replace("height=300", "height=1280").Replace("width=300", "width=1280").Replace("radius=75", "radius=250"), body + " as of " + DateTime.Now.ToString("MM/dd/yyy HH:mm:ss"));
                    }
                    break;

                case "/stock":
                    if (body.Length < 1 || body.Length > 5)
                    {
                        body = "^DJI";
                    }
                    protocol.SendStatusTyping(replyDestination);
                    protocol.SendImage(replyDestination, "https://chart.yahoo.com/t?s=" + body + "&lang=en-US&region=US&width=1200&height=765", "Chart for " + body + " as of " + DateTime.Now.ToString("MM/dd/yyy HH:mm:ss"));
                    break;

                case "/stock5":
                    if (body.Length < 1 || body.Length > 5)
                    {
                        body = "^DJI";
                    }
                    protocol.SendStatusTyping(replyDestination);
                    protocol.SendImage(replyDestination, "https://chart.yahoo.com/w?s=" + body + "&lang=en-US&region=US&width=1200&height=765", "5 day chart for " + body + " as of " + DateTime.Now.ToString("MM/dd/yyy HH:mm:ss"));
                    break;

                case "/stockyear":
                    if (body.Length < 1 || body.Length > 5)
                    {
                        body = "^DJI";
                    }
                    protocol.SendStatusTyping(replyDestination);
                    protocol.SendImage(replyDestination, "https://chart.yahoo.com/c/1y/" + body, "Year chart for " + body + " as of " + DateTime.Now.ToString("MM/dd/yyy HH:mm:ss"));
                    break;

                case "/translateto":
                    if (body == string.Empty)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Usage: /translateto <Language Code> <English Text>");
                        break;
                    }
                    protocol.SendStatusTyping(replyDestination);
                    var lang = body.Substring(0, body.IndexOf(" ", StringComparison.Ordinal));
                    var query = body.Substring(body.IndexOf(" ", StringComparison.Ordinal) + 1);
                    httpClient.AuthorizationHeader = "Basic " + bingKey;
                    dynamic dtto = JObject.Parse(httpClient.DownloadString("https://api.datamarket.azure.com/Bing/MicrosoftTranslator/v1/Translate?Text=%27" + Uri.EscapeDataString(query) + "%27&To=%27" + lang + "%27&$format=json").Result);
                    httpClient.AuthorizationHeader = string.Empty;
                    if (dtto.d == null || dtto.d.results == null || Enumerable.Count(dtto.d.results) < 1 || dtto.d.results[0].Text == null)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.  Try harder next time.");
                    }
                    else
                    {
                        protocol.SendPlainTextMessage(replyDestination, dtto.d.results[0].Text);
                    }
                    break;

                case "/translate":
                    if (body == string.Empty)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Usage: /translate <Foreign Text>");
                        break;
                    }
                    protocol.SendStatusTyping(replyDestination);
                    httpClient.AuthorizationHeader = "Basic " + bingKey;
                    dynamic dtrans = JObject.Parse(httpClient.DownloadString("https://api.datamarket.azure.com/Bing/MicrosoftTranslator/v1/Translate?Text=%27" + Uri.EscapeDataString(body) + "%27&To=%27en%27&$format=json").Result);
                    httpClient.AuthorizationHeader = string.Empty;
                    if (dtrans.d == null || dtrans.d.results == null || Enumerable.Count(dtrans.d.results) < 1 || dtrans.d.results[0].Text == null)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.  Try harder next time.");
                    }
                    else
                    {
                        protocol.SendPlainTextMessage(replyDestination, dtrans.d.results[0].Text);
                    }
                    break;

                case "/trixie":
                    if (body == string.Empty)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Usage: /trixie <Query>");
                        break;
                    }
                    protocol.SendStatusTyping(replyDestination);
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(httpClient.DownloadString("http://api.wolframalpha.com/v2/query?input=" + Uri.EscapeDataString(body) + "&appid=" + wolframAppId).Result);
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
                            if (subPodPlainText == null || subPodPlainText.InnerText.Trim().Length <= 0) continue;
                            var podName = pod.Attributes?["title"].Value.Trim();
                            if (podName == "Response" || podName == "Result")
                            {
                                stringBuilder.AppendLine(subPodPlainText.InnerText);
                            }
                            else
                            {
                                stringBuilder.AppendLine(podName + ": " + subPodPlainText.InnerText);
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
                    protocol.SendPlainTextMessage(replyDestination, "Trixie Is Best Pony Bot\r\nRelease fourty-two for .NET Core 1.0\r\nBy http://scottrfrost.github.io");
                    break;

                case "/weather":
                    if (body.Length < 2)
                    {
                        body = "Cincinnati, OH";
                    }
                    protocol.SendStatusTyping(replyDestination);
                    dynamic dwthr = JObject.Parse(httpClient.DownloadString("http://api.wunderground.com/api/" + wundergroundKey + "/conditions/q/" + body + ".json").Result);
                    if (dwthr.current_observation == null)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "You have disappointed Trixie.  \"" + body + "\" is bullshit and you know it.  Try \"City, ST\" or \"City, Country\" next time.");
                    }
                    else
                    {
                        protocol.SendPlainTextMessage(replyDestination,
                            dwthr.current_observation.display_location.full + " Conditions: " +
                            dwthr.current_observation.weather +
                            " Wind: " + dwthr.current_observation.wind_string +
                            " Temp: " + dwthr.current_observation.temperature_string + " Feels Like: " +
                            dwthr.current_observation.feelslike_string);
                    }
                    break;

                case "/wiki":
                    if (body == string.Empty)
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
                            protocol.SendMakdownMessage(replyDestination, "*" + page["title"] + "*\r\n" + page["extract"] + "\r\n" + "https://en.wikipedia.org/?curid=" + page["pageid"]);
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

                case "/ww":
                    var split = body.Split(' ');
                    if (split.Length != 4)
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Usage: /ww <carbs> <fat> <fiber> <protein>");
                        break;
                    }
                    try
                    {
                        var wwcarbs = Convert.ToDouble(split[0]);
                        var wwfat = Convert.ToDouble(split[1]);
                        var wwfiber = Convert.ToDouble(split[2]);
                        var wwprotein = Convert.ToDouble(split[3]);
                        protocol.SendPlainTextMessage(replyDestination, "WW PointsPlus value for " + wwcarbs + "g carbs, " + wwfat + "g fat, " + wwfiber + "g fiber, " + wwprotein + "g protein is: " + Math.Round((wwprotein / 10.9375) + (wwcarbs / 9.2105) + (wwfat / 3.8889) - (wwfiber / 12.5), 1));
                    }
                    catch
                    {
                        protocol.SendPlainTextMessage(replyDestination, "Trixie is disappointed that you used /ww incorrectly. The correct usage is: /ww <carbs> <fat> <fiber> <protein>");
                    }
                    break;
            }
        }
    }
}
