using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface IGoogleDriveService
    {
        Task<string> UploadFileAsync(IFormFile file);
    }
}
