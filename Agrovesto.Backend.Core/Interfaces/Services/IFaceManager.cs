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
        Task<ImageComparisonResponse> CompareFaces(IFormFile uploadedImage, ImageVerificationArgs args);
        Task<string> GetImageUrlAsync(string imageKey);
        Task<ImageUploadResponse> UploadImage(IFormFile image, string userId, string businessCode);
    }
}
