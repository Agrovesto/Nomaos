using Agrovesto.Backend.Core.Models.Common;
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
        Task<object> CompareFaces(IFormFile uploadedImage, ImageVerificationArgs args);
        Task<object> UploadImage(IFormFile image, string userId, string businessCode);
    }
}
