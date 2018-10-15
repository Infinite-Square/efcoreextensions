using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace EFCore.Extensions.SqlServer.UnitTests
{
    public class AppSettings
    {
        public string ConnectionString { get; set; }

        public static AppSettings Load()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build()
                .Get<AppSettings>();
        }
    }
}
