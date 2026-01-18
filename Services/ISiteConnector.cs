using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileDownloader2.Services
{
    public interface  ISiteConnector
    {
        bool Connect(); // Возвращает только bool
        
    }
}