using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{
    public interface IProfileImageStore
    {
        Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct);
        Task DeleteAsync(string key, CancellationToken ct);
        string GetUrl(string key);
    }

    public class ProfileImageOptions { public string Bucket { get; set; } = default!; }

    public class S3ProfileImageStore : IProfileImageStore
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;
        public S3ProfileImageStore(IAmazonS3 s3, IOptions<ProfileImageOptions> opts) { _s3 = s3; _bucket = opts.Value.Bucket; }

        public Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct) =>
            _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false
            }, ct);

        public Task DeleteAsync(string key, CancellationToken ct) => _s3.DeleteObjectAsync(_bucket, key, ct);

        // Private bucket, short-lived link; a fresh one is generated on every profile load
        public string GetUrl(string key) => _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(60)
        });
    }
}
