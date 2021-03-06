﻿using System.Threading.Tasks;

namespace TrixieBot
{
    public abstract class BaseProtocol
    {
        public Config config;

        protected BaseProtocol(Config config)
        {
            this.config = config;
        }

        abstract public Task<bool> Start();

        abstract public void SendFile(string destination, string Url, string filename = "", string referrer = "http://duckduckgo.com");

        abstract public void SendImage(string destination, string Url, string caption, string referrer = "http://duckduckgo.com");

        abstract public void SendHTMLMessage(string destination, string message);

        abstract public void SendLocation(string destination, float latitude, float longitude);

        abstract public void SendMarkdownMessage(string destination, string message);

        abstract public void SendPlainTextMessage(string destination, string message);

        abstract public void SendStatusTyping(string destination);

        abstract public void SendStatusUploadingFile(string destination);

        abstract public void SendStatusUploadingPhoto(string destination);
    }
}