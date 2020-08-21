using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EFCore.Extensions.SqlServer.UnitTests
{
    public class AppSettings
    {
        public string ConnectionString { get; set; }

        public static AppSettings Load()
        {
            var settings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build()
                .Get<AppSettings>();

            var tmp = Directory.GetCurrentDirectory(); // Path.GetTempPath();
            settings.ConnectionString = settings.ConnectionString.Replace("{tmp}", tmp);

            return settings;
        }
    }
}
