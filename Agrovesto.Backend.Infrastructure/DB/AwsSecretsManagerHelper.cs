namespace Agrovesto.Backend.Infrastructure
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.SecretsManager;
    using Amazon.SecretsManager.Model;
    using Newtonsoft.Json;

    public class AwsSecretsManagerHelper
    {
        private readonly string _secretName = "Agrovesto_Secrets"; // Replace with your secret name
        private readonly RegionEndpoint _region = RegionEndpoint.USEast2; // Replace with your AWS region

        public async Task<string> GetConnectionStringAsync()
        {
            using var client = new AmazonSecretsManagerClient(_region);

            try
            {
                var request = new GetSecretValueRequest { SecretId = _secretName };
                var response = await client.GetSecretValueAsync(request);

                if (response.SecretString != null)
                {
                    var secretJson = JsonConvert.DeserializeObject<SecretModel>(response.SecretString);
                    //"Server=my-rds-endpoint,1433;Database=mydatabase;User Id=myuser;Password=mypassword;TrustServerCertificate=True;\r\n";
                    return $"Server={secretJson.Host},{secretJson.Port};Database={secretJson.DbInstanceIdentifier};User Id={secretJson.Username};Password={secretJson.Password};TrustServerCertificate=True;";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving secret: {ex.Message}");
            }

            return string.Empty;
        }
        public string GetConnectionString()
        {
            return Task.Run(() => GetConnectionStringAsync()).GetAwaiter().GetResult();
        }
        private class SecretModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Engine { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public string DbInstanceIdentifier { get; set; }
        }
    }

}
