using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface IFaceManager
    {
        Task<object> CompareFaces(IFormFile uploadedImage, string userId, int targetImageId);
        Task<object> UploadImage(IFormFile image, string userId, string businessCode);
    }
}
