using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S3Object = Amazon.Rekognition.Model.S3Object;

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

            var awsAccessKey = configuration["AWS:AccessKey"];
            var awsSecretKey = configuration["AWS:SecretKey"];
            var region = configuration["AWS:Region"]; // Ensure this is set in appsettings.json

            if (string.IsNullOrEmpty(region))
            {
                throw new AmazonClientException("AWS Region is not configured properly.");
            }

            var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
            _rekognitionClient = new AmazonRekognitionClient(credentials, RegionEndpoint.GetBySystemName(region));

            _s3Client = new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(region));

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

            var targetImage = await GetImageFromS3(bucketName, targetImageRecord.ImageKey);
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
                return new { message = "Invalid image file." };
            }

            string bucketName = _configuration["AWS:BucketName"];
            if (string.IsNullOrEmpty(bucketName))
            {
                return new { message = "AWS S3 bucket is not configured." };
            }

            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await image.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            var existingImages = await _farmerRepository.GetImagesByBusinessCode(businessCode);

            foreach (var existingImage in existingImages)
            {
                var matchFound = await CompareFacesAsync(bucketName, existingImage.ImageKey, imageBytes);
                if (matchFound)
                {
                    return new { message = "A user with the same face already exists." };
                }
            }
            string imageKey = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";


            using (var memoryStream = new MemoryStream(imageBytes))
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = imageKey,
                    InputStream = memoryStream,
                    ContentType = image.ContentType
                };
                await _s3Client.PutObjectAsync(putRequest);
            }

            var savedImage = await _farmerRepository.SaveImage(new Core.Models.Businesses.FarmerImagesModel
            {
                BelongToBusinessCode = businessCode,
                FarmerUserId = userId,
                ImageKey = imageKey,
                ImageName = image.FileName
            });

            return new { message = "Image uploaded successfully", imageKey = imageKey };
        }
        private async Task<bool> CompareFacesAsync(string bucketName, string existingImageKey, byte[] newImageBytes)
        {
            try
            {
                using (var memoryStream = new MemoryStream(newImageBytes))
                {
                    var compareRequest = new CompareFacesRequest
                    {
                        SourceImage = new Image
                        {
                            Bytes = memoryStream
                        },
                        TargetImage = new Image
                        {
                            S3Object = new S3Object
                            {
                                Bucket = bucketName,
                                Name = existingImageKey
                            }
                        },
                        SimilarityThreshold = 90 // Adjust similarity threshold as needed
                    };

                    var response = await _rekognitionClient.CompareFacesAsync(compareRequest);

                    return response.FaceMatches.Count > 0;
                }
            }
            catch (AmazonRekognitionException ex)
            {
                Console.WriteLine($"Rekognition Error: {ex.Message}");
                return false;
            }
        }

        //public async Task<object> UploadImage(IFormFile image, string userId, string businessCode)
        //{
        //    if (image == null || image.Length == 0)
        //    {
        //        return null;
        //    }

        //    string bucketName = _configuration["AWS:BucketName"];
        //    if (string.IsNullOrEmpty(bucketName))
        //    {
        //        return null;
        //    }

        //    string imageKey = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

        //    using (var memoryStream = new MemoryStream())
        //    {
        //        await image.CopyToAsync(memoryStream);
        //        var putRequest = new PutObjectRequest
        //        {
        //            BucketName = bucketName,
        //            Key = imageKey,
        //            InputStream = memoryStream,
        //            ContentType = image.ContentType
        //        };
        //        await _s3Client.PutObjectAsync(putRequest);
        //    }

        //    var targetImage = await _farmerRepository.SaveImage(new Core.Models.Businesses.FarmerImagesModel
        //    {
        //        BelongToBusinessCode =  businessCode,
        //        FarmerUserId =  userId,
        //        ImageKey = imageKey,
        //        ImageName = image.FileName

        //    });

        //    return new { message = "Image uploaded successfully", imageKey = imageKey };
        //}

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
