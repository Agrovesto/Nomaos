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
    }

}
