using Agrovesto.Backend.Core.Models.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Services
{
    public interface IAWSRekognitionManager
    {
        Task<ImageComparisonResponse> CompareFaces(IFormFile uploadedImage, ImageVerificationArgs args);
        Task<(byte[] FileBytes, string ContentType, string FileName)> GetFileAsync(string fileKey);
        Task<string> GetImageUrlAsync(string imageKey);
        Task<string> UploadFileAsync(IFormFile file, string folderName);
        Task<ImageUploadResponse> UploadImage(IFormFile image, string userId, string businessCode, string? organizationCode);
    }
}
