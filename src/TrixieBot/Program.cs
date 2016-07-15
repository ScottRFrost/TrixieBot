using System;
using System.Threading;

namespace TrixieBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " Starting Up");
            while (true)
            {
                try
                {
                    var trixeBot = new TrixieBot();
                    trixeBot.Start().Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now.ToString("M/d HH:mm") + " MAIN LOOP EXIT ERROR - " + ex);
                    Thread.Sleep(30000);
                }
            }
        }
    }
}