using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agrovesto.Backend.Core.Interfaces.Services;
using Microsoft.Extensions.Hosting;

namespace Agrovesto.Backend.Infrastructure.ExternalServices
{
    public class GoogleDriveService : IGoogleDriveService
    {
        //
        private readonly DriveService _driveService;
        private readonly string _jsonFilePath;
        public GoogleDriveService(IConfiguration config, IHostEnvironment environment)
        {
            string relativePath = config["GoogleCredFilePath"];
            _jsonFilePath = Path.Combine(environment.ContentRootPath, relativePath);

          //  var credentialsPath = "client_secret.json"; // Your service account key
            GoogleCredential credential;

            using (var stream = new FileStream(_jsonFilePath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(DriveService.Scope.Drive);
            }

            _driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Agrovesto",
            });
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            try
            {
                var listRequest = _driveService.Files.List();
                listRequest.Q = "mimeType='application/vnd.google-apps.folder'";
                listRequest.Fields = "files(id, name)";
                var files = await listRequest.ExecuteAsync();

                foreach (var i in files.Files)
                {
                    Console.WriteLine($"Folder Name: {i.Name} | ID: {i.Id}");
                }
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = file.FileName,
                    Parents = new List<string> { "1s_Tl0A7uoGWOpKcrtrekz4u0NR3btRH1" }
                };

                using var stream = file.OpenReadStream();
                var request = _driveService.Files.Create(fileMetadata, stream, file.ContentType);
                request.Fields = "id";
                var uploadedFile = await request.UploadAsync();

                if (uploadedFile.Status == Google.Apis.Upload.UploadStatus.Completed)
                {
                    var fileId = request.ResponseBody.Id;

                    // Make the file public
                    var permission = new Permission
                    {
                        Role = "reader",
                        Type = "anyone"
                    };
                    await _driveService.Permissions.Create(permission, fileId).ExecuteAsync();

                    return $"https://drive.google.com/uc?id={fileId}";
                }
                throw new Exception();
            }

            catch(Exception ex) {

                throw new Exception("Image upload failed");
            }
        }


        public async Task<string> UploadFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file upload request");

            try
            {
                var folderId = await GetOrCreateFolderAsync(folderName);

                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = file.FileName,
                    Parents = new List<string> { folderId }
                };

                using var stream = file.OpenReadStream();
                var request = _driveService.Files.Create(fileMetadata, stream, file.ContentType);
                request.Fields = "id";
                var uploadResult = await request.UploadAsync();

                if (uploadResult.Status != Google.Apis.Upload.UploadStatus.Completed)
                    throw new Exception("File upload failed");

                var fileId = request.ResponseBody.Id;

             
                var permission = new Permission
                {
                    Role = "reader",
                    Type = "anyone"
                };
                await _driveService.Permissions.Create(permission, fileId).ExecuteAsync();

              
                return $"https://drive.google.com/uc?id={fileId}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Google Drive upload failed: {ex.Message}", ex);
            }
        }

        private async Task<string> GetOrCreateFolderAsync(string folderName)
        {
            // Try to find the folder first
            var listRequest = _driveService.Files.List();
            listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false";
            listRequest.Fields = "files(id, name)";
            var folders = await listRequest.ExecuteAsync();

            var folder = folders.Files.FirstOrDefault();
            if (folder != null)
                return folder.Id;

            // Folder doesn’t exist — create it
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            var createRequest = _driveService.Files.Create(fileMetadata);
            createRequest.Fields = "id";
            var createdFolder = await createRequest.ExecuteAsync();
            return createdFolder.Id;
        }
    }

}


