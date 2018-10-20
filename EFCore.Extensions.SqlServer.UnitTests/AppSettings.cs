using Microsoft.Extensions.Configuration;
using System.IO;

namespace EFCore.Extensions.SqlServer.UnitTests
{
    public class AppSettings
    {
        public string ConnectionString { get; set; }

        public static AppSettings Load()
        {
            var appSettings = new AppSettings();
            string directoryPath = Path.GetTempPath();
            appSettings.ConnectionString =  $"Data Source=(localdb)\\MSSQLLocalDB;AttachDBFilename={directoryPath}efcoreextensions.mdf;Initial Catalog=efcoreextensions;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            return appSettings;
        }
    }
}
