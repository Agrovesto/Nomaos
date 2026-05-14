using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Logic.Managers;
using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S3Object = Amazon.Rekognition.Model.S3Object;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.ImageRecognition
{
    public class AWSRekgnitionManager : IAWSRekognitionManager
    {
        private readonly AmazonRekognitionClient _rekognitionClient;
        private readonly AmazonS3Client _s3Client;
        private readonly IConfiguration _configuration;
        private readonly IFarmerRepository _farmerRepository;
        private const float SimilarityThreshold = 70F;
        private readonly ILogger<AWSRekgnitionManager> _log;
        private readonly string _bucketName;


        public AWSRekgnitionManager(
    IConfiguration configuration,
    ILogger<AWSRekgnitionManager> log,
    IFarmerRepository farmerRepository)
        {
            _log = log;

            Console.WriteLine("========== AWS CONFIG DEBUG ==========");

            foreach (var item in configuration.AsEnumerable())
            {
                if (item.Key.StartsWith("AWS"))
                {
                    Console.WriteLine($"{item.Key} = {item.Value}");
                }
            }

            _bucketName = configuration["AWS:BucketName"];

            var awsAccessKey = configuration["AWS:AccessKey"];
            var awsSecretKey = configuration["AWS:SecretKey"];
            var region = configuration["AWS:Region"];

            Console.WriteLine($"Region = {region}");

            if (string.IsNullOrEmpty(region))
            {
                throw new AmazonClientException("AWS Region is not configured properly.");
            }

            var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);

            _rekognitionClient = new AmazonRekognitionClient(
                credentials,
                RegionEndpoint.GetBySystemName(region));

            _s3Client = new AmazonS3Client(
                credentials,
                RegionEndpoint.GetBySystemName(region));

            _configuration = configuration;
            _farmerRepository = farmerRepository;
        }
        //public AWSRekgnitionManager(IConfiguration configuration, ILogger<AWSRekgnitionManager> log, IFarmerRepository farmerRepository)
        //{
        //    _log = log;
        //    _bucketName = configuration["AWS:BucketName"];
        //    var awsAccessKey = configuration["AWS:AccessKey"];
        //    var awsSecretKey = configuration["AWS:SecretKey"];
        //    var region = configuration["AWS:Region"]; // Ensure this is set in appsettings.json

        //    if (string.IsNullOrEmpty(region))
        //    {
        //        throw new AmazonClientException("AWS Region is not configured properly.");
        //    }

        //    var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
        //    _rekognitionClient = new AmazonRekognitionClient(credentials, RegionEndpoint.GetBySystemName(region));

        //    _s3Client = new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(region));

        //    _configuration = configuration;
        //    _farmerRepository = farmerRepository;
        //}

        public async Task<(byte[] FileBytes, string ContentType, string FileName)> GetFileAsync(string fileKey)
        {
            if (string.IsNullOrEmpty(fileKey))
                throw new ArgumentException("File key cannot be null or empty.", nameof(fileKey));

            try
            {
                string bucketName = _configuration["AWS:BucketName"];
                if (string.IsNullOrEmpty(bucketName))
                    throw new Exception("AWS S3 bucket is not configured.");

                var response = await _s3Client.GetObjectAsync(bucketName, fileKey);

                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await response.ResponseStream.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                return (FileBytes: fileBytes,
                        ContentType: response.Headers.ContentType,
                        FileName: Path.GetFileName(fileKey));
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new Exception($"File with key '{fileKey}' not found in S3 bucket.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching file from S3: {ex.Message}");
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
            {
                return "Invalid file.";
            }

            try
            {
                string bucketName = _configuration["AWS:BucketName"];
                if (string.IsNullOrEmpty(bucketName))
                {
                    return "AWS S3 bucket is not configured.";
                }

                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                using var md5 = System.Security.Cryptography.MD5.Create();
                var fileHash = BitConverter.ToString(md5.ComputeHash(fileBytes)).Replace("-", "").ToLower();

                string keyPrefix = string.IsNullOrEmpty(folderName) ? "" : $"{folderName}/";

                var listRequest = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = keyPrefix
                };

                var existingObjects = await _s3Client.ListObjectsV2Async(listRequest);

                foreach (var obj in existingObjects.S3Objects)
                {
                    var metadata = await _s3Client.GetObjectMetadataAsync(bucketName, obj.Key);

                    string existingHash = null;
                    if (metadata.Metadata.Keys.Contains("x-amz-meta-filehash"))
                    {
                        existingHash = metadata.Metadata["x-amz-meta-filehash"];
                    }

                    if (existingHash != null && existingHash == fileHash)
                    {
                        return obj.Key;
                    }

                }
               // string fileKey = $"{keyPrefix}{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                string fileKey = $"{folderName}{Path.GetExtension(file.FileName)}";

                using (var memoryStream = new MemoryStream(fileBytes))
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = fileKey,
                        InputStream = memoryStream,
                        ContentType = file.ContentType
                    };

                    putRequest.Metadata.Add("FileHash", fileHash);

                    await _s3Client.PutObjectAsync(putRequest);
                }

                return fileKey;
            }
            catch (Exception ex)
            {
                throw new Exception($"Upload file error: {ex.Message}");
            }
        }

        //public async Task<string> UploadFileAsync(IFormFile file, string folderName)
        //{
        //    if (file == null || file.Length == 0)
        //    {
        //        return "Invalid file.";
        //    }

        //    try
        //    {
        //        string bucketName = _configuration["AWS:BucketName"];
        //        if (string.IsNullOrEmpty(bucketName))
        //        {
        //            return  "AWS S3 bucket is not configured." ;
        //        }

        //        byte[] fileBytes;
        //        using (var memoryStream = new MemoryStream())
        //        {
        //            await file.CopyToAsync(memoryStream);
        //            fileBytes = memoryStream.ToArray();
        //        }

        //        //var existingFile = await _repository.GetUserFile(userId, businessCode, organizationCode);
        //        //if (existingFile != null)
        //        //{
        //        //    try
        //        //    {
        //        //        var deleteRequest = new DeleteObjectRequest
        //        //        {
        //        //            BucketName = bucketName,
        //        //            Key = existingFile.FileKey
        //        //        };
        //        //        await _s3Client.DeleteObjectAsync(deleteRequest);
        //        //    }
        //        //    catch (Exception ex)
        //        //    {
        //        //        throw new Exception($"Error deleting existing file: {ex.Message}");
        //        //    }
        //        //}

        //        string fileKey = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        //        using (var memoryStream = new MemoryStream(fileBytes))
        //        {
        //            var putRequest = new PutObjectRequest
        //            {
        //                BucketName = bucketName,
        //                Key = fileKey,
        //                InputStream = memoryStream,
        //                ContentType = file.ContentType
        //            };
        //            await _s3Client.PutObjectAsync(putRequest);
        //        }

        //        return fileKey;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Upload file error: {ex.Message}");
        //    }
        //}


        public async Task<ImageComparisonResponse> CompareFaces(IFormFile uploadedImage, ImageVerificationArgs args)
        {
            try
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

                var targetImageRecord = await _farmerRepository.GetFarmerImage(args.FarmerUserId, args.BusinessCode, args.OrganizationCode);
                if (targetImageRecord == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Image Record Found for the specified farmer");
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
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Image Record Found, Kindy capture farmer image");
                }

                var compareFacesRequest = new CompareFacesRequest
                {
                    SourceImage = sourceImage,
                    TargetImage = targetImage,
                    SimilarityThreshold = SimilarityThreshold
                };

                var compareFacesResponse = await _rekognitionClient.CompareFacesAsync(compareFacesRequest);
                bool isMatch = compareFacesResponse.FaceMatches.Count > 0;

                return new ImageComparisonResponse
                {
                    IsSimilarFace = isMatch,
                    ImageKey = targetImageRecord.ImageKey,
                    SimilarityPercentage = isMatch ? compareFacesResponse.FaceMatches[0].Similarity : 0
                };
            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Inventory: {ex.Message}");

                throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, $"No Record Found, Kindy capture farmer image");
            }
            catch (Exception ex)
            {
                throw new Exception($"Recognition Error: {ex.Message}");
            }
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

        public async Task<string> GetImageUrlAsync(string imageKey)
        {
            string bucketName = _configuration["AWS:BucketName"];
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = imageKey,
                    Expires = DateTime.UtcNow.AddHours(1) // URL expires in 1 hour
                };

                return _s3Client.GetPreSignedURL(request);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating S3 URL: {ex.Message}");
            }
        }

        public async Task<ImageUploadResponse> UploadImage(IFormFile image, string userId, string businessCode, string? organizationCode)
        {
            if (image == null || image.Length == 0)
            {
                return new ImageUploadResponse { Message = "Invalid image file.", ImageKey = null };
            }
            string[] allowedFormats = { ".jpg", ".jpeg", ".png" };
            string fileExtension = Path.GetExtension(image.FileName).ToLower();

            if (!allowedFormats.Contains(fileExtension))
            {
                return new ImageUploadResponse { Message = "Unsupported image format. Use JPG, JPEG, or PNG.", ImageKey = null };
            }
            try
            {
                string bucketName = _configuration["AWS:BucketName"];
                if (string.IsNullOrEmpty(bucketName))
                {
                    return new ImageUploadResponse { Message = "AWS S3 bucket is not configured.", ImageKey = null };
                }

                byte[] imageBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await image.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }
                var imageExist = await _farmerRepository.GetUserImage(userId, businessCode, organizationCode);
                if (imageExist != null)
                {
                    try
                    {
                        var deleteRequest = new DeleteObjectRequest
                        {
                            BucketName = bucketName,
                            Key = imageExist.ImageKey
                        };
                        await _s3Client.DeleteObjectAsync(deleteRequest);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error deleting existing image: {ex.Message}");
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
                    OrganizationCode = organizationCode,
                    ImageName = image.FileName
                });

                return new ImageUploadResponse { Message = "Image uploaded successfully", ImageKey = imageKey };
            }
            catch (AmazonRekognitionException ex)
            {
                throw new Exception($"Recognition Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Upload Image error: {ex.Message}");
            }
        }
        private async Task<bool> DoesS3ObjectExistAsync(string bucketName, string objectKey)
        {
            try
            {
                await _s3Client.GetObjectMetadataAsync(bucketName, objectKey);
                return true; // Object exists
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false; // Object does not exist
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking if object exists in S3: {ex.Message}");
            }
        }

        private async Task<bool> CompareFacesAsync(string bucketName, string existingImageKey, byte[] newImageBytes)
        {
            if (newImageBytes == null || newImageBytes.Length == 0)
            {
                throw new Exception("Source image data is empty.");
            }

            try
            {
                // 🔹 Validate that the target image exists in S3
                bool objectExists = await DoesS3ObjectExistAsync(bucketName, existingImageKey);
                if (!objectExists)
                {
                    throw new Exception($"Target image '{existingImageKey}' does not exist in S3.");
                }

                var compareRequest = new CompareFacesRequest
                {
                    SourceImage = new Image
                    {
                        Bytes = new MemoryStream(newImageBytes) // ✅ Convert properly
                    },
                    TargetImage = new Image
                    {
                        S3Object = new S3Object
                        {
                            Bucket = bucketName,
                            Name = existingImageKey
                        }
                    },
                    SimilarityThreshold = 90 // Adjust as needed
                };

                var response = await _rekognitionClient.CompareFacesAsync(compareRequest);

                return response.FaceMatches.Count > 0;
            }
            catch (AmazonRekognitionException ex)
            {
                throw new Exception($"Recognition Error: {ex.Message}");
            }
        }

    }
}
