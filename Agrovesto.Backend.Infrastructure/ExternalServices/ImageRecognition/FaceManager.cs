using Amazon.Rekognition.Model;
using Amazon.Rekognition;
using Amazon.S3.Model;
using Amazon.S3;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.ImageRecognition
{

    public class FaceManager : IFaceManager
    {
        private readonly AmazonRekognitionClient _rekognitionClient;
        private readonly AmazonS3Client _s3Client;
        private readonly IConfiguration _configuration;
        private readonly IFarmerRepository _farmerRepository;
        private const float SimilarityThreshold = 70F;

        public FaceManager(IConfiguration configuration, IFarmerRepository farmerRepository)
        {
            _rekognitionClient = new AmazonRekognitionClient();
            _s3Client = new AmazonS3Client();
            _configuration = configuration;
            _farmerRepository = farmerRepository;
        }

        public async Task<object> CompareFaces(IFormFile uploadedImage, string userId, int targetImageId)
        {
            if (uploadedImage == null || uploadedImage.Length == 0)
            {
                return null;
            }

            string bucketName = _configuration["AWS:BucketName"];
            if (string.IsNullOrEmpty(bucketName))
            {
                return null;
            }

            var targetImageRecord = await _farmerRepository.GetFarmerImage(userId);
            if (targetImageRecord == null)
            {
                return null;
            }


            Amazon.Rekognition.Model.Image sourceImage;
            using (var memoryStream = new MemoryStream())
            {
                await uploadedImage.CopyToAsync(memoryStream);
                sourceImage = new Amazon.Rekognition.Model.Image { Bytes = memoryStream };
            }

            var targetImage = await GetImageFromS3(bucketName, targetImageRecord.IMageKey);
            if (targetImage == null)
            {
                return null;
            }

            var compareFacesRequest = new CompareFacesRequest
            {
                SourceImage = sourceImage,
                TargetImage = targetImage,
                SimilarityThreshold = SimilarityThreshold
            };

            var compareFacesResponse = await _rekognitionClient.CompareFacesAsync(compareFacesRequest);
            bool isMatch = compareFacesResponse.FaceMatches.Count > 0;

            return new { match = isMatch, similarity = isMatch ? compareFacesResponse.FaceMatches[0].Similarity : 0 };
        }
        public async Task<object> UploadImage(IFormFile image, string userId, string businessCode)
        {
            if (image == null || image.Length == 0)
            {
                return null;
            }

            string bucketName = _configuration["AWS:BucketName"];
            if (string.IsNullOrEmpty(bucketName))
            {
                return null;
            }
            
            string imageKey = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

            using (var memoryStream = new MemoryStream())
            {
                await image.CopyToAsync(memoryStream);
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = imageKey,
                    InputStream = memoryStream,
                    ContentType = image.ContentType
                };
                await _s3Client.PutObjectAsync(putRequest);
            }

            var targetImage = await _farmerRepository.SaveImage(new Core.Models.Businesses.FarmerImagesModel
            {
                BelongToBusinessCode= businessCode,
                FarmerUserId=userId,
                IMageKey=imageKey,
                ImageName=image.FileName

            }); 

            return new { message = "Image uploaded successfully", imageKey = imageKey };
        }

        private async Task<Amazon.Rekognition.Model.Image> GetImageFromS3(string bucket, string key)
        {
            try
            {
                var response = await _s3Client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucket,
                    Key = key
                });

                using (var memoryStream = new MemoryStream())
                {
                    await response.ResponseStream.CopyToAsync(memoryStream);
                    return new Amazon.Rekognition.Model.Image { Bytes = memoryStream };
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
