using System;
using Microsoft.Extensions.Configuration;

namespace EasySeating
{
    // http://blog.shibayan.jp/entry/20180203/1517589893
    public class Settings
    {
        private Settings()
        {
            var builder = new ConfigurationBuilder()
                            .AddJsonFile("local.settings.json", true)
                            .AddEnvironmentVariables();

            _configuration = builder.Build();
        }

        private readonly IConfigurationRoot _configuration;
        public static Settings Instance { get; } = new Settings();

        public string SlackAPIToken => _configuration["SlackAPIKey"];
        public string VerifiedToken => _configuration["Token"];
    }
}