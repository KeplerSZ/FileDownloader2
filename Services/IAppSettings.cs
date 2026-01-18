using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileDownloader2.Services
{
    public interface IAppSettings
    {
        // SitesSettings
    string FileUrl { get; }
    string DownloadBaseUrl { get; }
    string Username { get; }
    string Password { get; }
    string TestUrl { get; }
    
    // DownloadSettings  
    string DownloadFolder { get; }
    string LogFolder { get; }
    }
}