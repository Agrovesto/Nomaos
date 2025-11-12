using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace Agrovesto.Backend.Infrastructure.ExternalServices
{
    /// <summary>
    /// Loads a JSON secret from AWS Secrets Manager and merges the values into the application's configuration
    /// as in-memory entries so existing configuration lookups work unchanged.
    /// - Secret name is read from configuration key: "AWS:AppSettingsSecretName" (fallback: "agrovesto/appsettings/development").
    /// - AWS creds/region are read from configuration keys: "AWS:AccessKey", "AWS:SecretKey", "AWS:Region".
    /// If credentials are not present the default AWS credential chain will be used.
    /// </summary>
    public static class AwsSecretsConfigurator
    {
        // Backward-compatible sync wrapper (calls the async implementation)
        public static void AddSecretsToConfiguration(ConfigurationManager configuration)
        {
            // Call the async implementation synchronously for callers that don't await
            AddSecretsToConfigurationAsync(configuration).GetAwaiter().GetResult();
        }

        public static async System.Threading.Tasks.Task AddSecretsToConfigurationAsync(ConfigurationManager configuration)
        {
            if (configuration == null) return;

            var secretName = configuration["AWSConfig:AppSettingsSecretName"] ?? configuration["AWSConfig:AppSettingsSecret"] ?? "agrovesto/appsettings/development";
            if (string.IsNullOrWhiteSpace(secretName)) return;

            string accessKey = configuration["AWS:AccessKey"];
            string secretKey = configuration["AWS:SecretKey"];
            string sessionToken = configuration["AWS:SessionToken"]; // support temporary creds
            string region = configuration["AWS:Region"];

            IAmazonSecretsManager client = null;
            try
            {
                if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
                {
                    AWSCredentials creds = string.IsNullOrEmpty(sessionToken)
                        ? new BasicAWSCredentials(accessKey, secretKey)
                        : new SessionAWSCredentials(accessKey, secretKey, sessionToken);

                    var regionEndpoint = !string.IsNullOrEmpty(region) ? RegionEndpoint.GetBySystemName(region) : RegionEndpoint.USEast1;
                    client = new AmazonSecretsManagerClient(creds, regionEndpoint);
                }
                else
                {
                    if (!string.IsNullOrEmpty(region))
                    {
                        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
                        client = new AmazonSecretsManagerClient(regionEndpoint);
                    }
                    else
                    {
                        client = new AmazonSecretsManagerClient();
                    }
                }

                var getReq = new GetSecretValueRequest { SecretId = secretName };
                var secretResp = await client.GetSecretValueAsync(getReq).ConfigureAwait(false);

                string secretString = secretResp.SecretString;
                if (string.IsNullOrEmpty(secretString) && secretResp.SecretBinary != null)
                {
                    secretString = Encoding.UTF8.GetString(secretResp.SecretBinary.ToArray());
                }

                if (string.IsNullOrWhiteSpace(secretString)) return;

                using var doc = JsonDocument.Parse(secretString);
                var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                FlattenJsonElement(doc.RootElement, prefix: string.Empty, data);

                // Merge into existing configuration as in-memory collection so lookups like Configuration["Jwt:Key"] work
                configuration.AddInMemoryCollection(data);
            }
            catch (AmazonSecretsManagerException ex)
            {
                // Log AWS-specific errors to standard error so startup problems are visible
                Console.Error.WriteLine($"Unable to load AWS secret '{secretName}': {ex.ErrorCode} - {ex.Message}");
            }
            catch (AmazonServiceException ex)
            {
                Console.Error.WriteLine($"AWS service error when loading secret '{secretName}': {ex.ErrorCode} - {ex.Message}");
            }
            catch (Exception ex)
            {
                // Fail softly: do not throw during configuration build. If secrets are required, app should handle missing values.
                Console.Error.WriteLine($"Failed to load AWS secret '{secretName}': {ex.Message}");
            }
            finally
            {
                client?.Dispose();
            }
        }

        private static void FlattenJsonElement(JsonElement element, string prefix, IDictionary<string, string> result)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}:{prop.Name}";
                        FlattenJsonElement(prop.Value, key, result);
                    }
                    break;
                case JsonValueKind.Array:
                    int i = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        var key = string.IsNullOrEmpty(prefix) ? i.ToString() : $"{prefix}:{i}";
                        FlattenJsonElement(item, key, result);
                        i++;
                    }
                    break;
                case JsonValueKind.Null:
                    result[prefix] = null;
                    break;
                default:
                    result[prefix] = element.ToString();
                    break;
            }
        }
    }
}
