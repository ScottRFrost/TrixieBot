using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace TrixieBot
{
    public class ProHttpClient : HttpClient
    {
        public ProHttpClient()
        {
            Timeout = new TimeSpan(0, 0, 30);
            ReferrerUri = "https://duckduckgo.com";
            AuthorizationHeader = string.Empty;
            DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
            DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5");
            DefaultRequestHeaders.TryAddWithoutValidation("Connection", "keep-alive");
            DefaultRequestHeaders.TryAddWithoutValidation("DNT", "1");
            DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:51.0) Gecko/20100101 Firefox/56.0");
            CustomHeaders = new Dictionary<string, string>();
        }

        public string AuthorizationHeader { get; set; }

        public string BingHeader { get; set; }

        public string ReferrerUri { get; set; }
        
        public Dictionary<string,string> CustomHeaders {get; set;}

        public void AddHeader (string name, string value) {
            CustomHeaders.Add(name,value);
            DefaultRequestHeaders.TryAddWithoutValidation(name, value);
        }

        public async Task<string> DownloadString(string uri)
        {
            BuildHeaders();
            try{
                var response = await GetStringAsync(uri);
                return response;
            }
            catch (System.Net.Http.HttpRequestException ex){
                Console.Write(ex.ToString());
                return ex.Message;
            }
            finally{
                CleanHeaders();
            }
        }

        public async Task<Stream> DownloadData(string uri)
        {
            BuildHeaders();
            try{
                var response = await GetStreamAsync(uri);
                return response;
            }
            catch (System.Net.Http.HttpRequestException ex){
                Console.Write(ex.ToString());
                throw;
            }
            finally{
                CleanHeaders();
            }
        }

        void BuildHeaders()
        {
            if(ReferrerUri != string.Empty) 
            {
                DefaultRequestHeaders.Referrer = new Uri(ReferrerUri);
            }
            if (AuthorizationHeader != string.Empty)
            {
                DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AuthorizationHeader);
            }
            if (BingHeader != string.Empty)
            {
                DefaultRequestHeaders.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", BingHeader);
            }
            foreach(var header in CustomHeaders){
                DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                Console.WriteLine(header.Key + ":" + header.Value);
            }
        }

        void CleanHeaders()
        {
            ReferrerUri = "https://duckduckgo.com";
            AuthorizationHeader = string.Empty;
            DefaultRequestHeaders.Remove("Authorization");
            foreach(var header in CustomHeaders){
                DefaultRequestHeaders.Remove(header.Key);
            }
            CustomHeaders.Clear();
        }
    }
}
