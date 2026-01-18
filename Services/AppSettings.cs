using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileDownloader2.Services
{
    public class AppSettings : IAppSettings
    {
        public string FileUrl { get; set; }
        public string DownloadBaseUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string TestUrl { get; set; }
        public string DownloadFolder { get; set; }
        public string LogFolder { get; set; }
    }
}