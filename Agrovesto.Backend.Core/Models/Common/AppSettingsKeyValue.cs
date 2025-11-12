using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;


namespace Agrovesto.Backend.Core.Models.Common
{
  
        public class AppSettingsDevelopment
        {
            [JsonPropertyName("ConnectionStrings")]
            public ConnectionStrings ConnectionStrings { get; set; }

            [JsonPropertyName("EPPlus")]
            public EPPlusOptions EPPlus { get; set; }

            [JsonPropertyName("JsonFilePath")]
            public string JsonFilePath { get; set; }

            [JsonPropertyName("FarmerCategoryPath")]
            public string FarmerCategoryPath { get; set; }

            [JsonPropertyName("GoogleCredFilePath")]
            public string GoogleCredFilePath { get; set; }

            [JsonPropertyName("AesOptions")]
            public AesOptions AesOptions { get; set; }

            [JsonPropertyName("IdentityPass")]
            public IdentityPassOptions IdentityPass { get; set; }

            [JsonPropertyName("AWS")]
            public AWSOptions AWS { get; set; }

            [JsonPropertyName("MarketMeasurementStandard")]
            public MarketMeasurementStandardOptions MarketMeasurementStandard { get; set; }

            [JsonPropertyName("Sterling")]
            public SterlingOptions Sterling { get; set; }

            [JsonPropertyName("Jwt")]
            public JwtOptions Jwt { get; set; }

            [JsonPropertyName("Serilog")]
            public SerilogOptions Serilog { get; set; }

            [JsonPropertyName("SendGrid")]
            public SendGridOptions SendGrid { get; set; }

            [JsonPropertyName("ResetPasswordLink")]
            public string ResetPasswordLink { get; set; }

            [JsonPropertyName("Propagation")]
            public PropagationOptions Propagation { get; set; }

            [JsonPropertyName("bcc")]
            public BccOptions Bcc { get; set; }

            [JsonPropertyName("Twilio")]
            public TwilioOptions Twilio { get; set; }

            [JsonPropertyName("TermiSMS")]
            public TermiSMSOptions TermiSMS { get; set; }

            [JsonPropertyName("AfricasTalking")]
            public AfricasTalkingOptions AfricasTalking { get; set; }

            [JsonPropertyName("Location")]
            public LocationOptions Location { get; set; }

            [JsonPropertyName("WhisperSMS")]
            public WhisperSMSOptions WhisperSMS { get; set; }

            [JsonPropertyName("CorsOrigins")]
            public string CorsOrigins { get; set; }

            [JsonPropertyName("AppLogs")]
            public AppLogsOptions AppLogs { get; set; }

            [JsonPropertyName("Logging")]
            public LoggingOptions Logging { get; set; }
        }

        public class ConnectionStrings
        {
            [JsonPropertyName("DefaultConnection_")]
            public string DefaultConnection_ { get; set; }

            [JsonPropertyName("DefaultConnection")]
            public string DefaultConnection { get; set; }

            [JsonPropertyName("DefaultConnection___")]
            public string DefaultConnection___ { get; set; }

            [JsonPropertyName("DefaultConnection__")]
            public string DefaultConnection__ { get; set; }
        }

        public class EPPlusOptions
        {
            [JsonPropertyName("ExcelPackage")]
            public ExcelPackageOptions ExcelPackage { get; set; }
        }

        public class ExcelPackageOptions
        {
            [JsonPropertyName("LicenseContext")]
            public string LicenseContext { get; set; }
        }

        public class IdentityPassOptions
        {
            public string Url { get; set; }

            [JsonPropertyName("IdentityPayUrl")]
            public string IdentityPayUrl { get; set; }

            [JsonPropertyName("Public_Key")]
            public string PublicKey { get; set; }

            [JsonPropertyName("Secret_Key")]
            public string SecretKey { get; set; }

            [JsonPropertyName("App_Id")]
            public string AppId { get; set; }

            [JsonPropertyName("preembleyurl")]
            public string PreembleyUrl { get; set; }

            [JsonPropertyName("preembleyPublicKey")]
            public string PreembleyPublicKey { get; set; }

            [JsonPropertyName("preembleySecretKey")]
            public string PreembleySecretKey { get; set; }

            [JsonPropertyName("preembleyAppId")]
            public string PreembleyAppId { get; set; }
        }

        public class AWSOptions
        {
            public string Region { get; set; }
            public string Profile { get; set; }
            public string AccessKey { get; set; }
            public string SecretKey { get; set; }
            public string BucketName { get; set; }
        }

        public class MarketMeasurementStandardOptions
        {
            public decimal BagToMudu { get; set; }
            public decimal BagToKg { get; set; }
            public decimal MuduToKg { get; set; }
            public decimal TonsToBag { get; set; }
            public decimal TonsToKg { get; set; }
            public decimal TonsToMudus { get; set; }
        }

        public class SterlingOptions
        {
            public decimal Charge { get; set; }
        }

        public class JwtOptions
        {
            public string Audience { get; set; }
            public string Issuer { get; set; }
            public string Key { get; set; }
        }

        public class SerilogOptions
        {
            public string LogLevel { get; set; }
            public string OutputTemplate { get; set; }
        }

        public class SendGridOptions
        {
            [JsonPropertyName("key")]
            public string Key { get; set; }
        }

        public class PropagationOptions
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
        }

        public class BccOptions
        {
            public string email { get; set; }
        }

        public class TwilioOptions
        {
            public string number { get; set; }
            public string sid { get; set; }
            public string authToken { get; set; }
        }

        public class TermiSMSOptions
        {
            public string livekey { get; set; }
            public string testkey { get; set; }
            public string from { get; set; }
            public string type { get; set; }
            public string channel { get; set; }
            public string url { get; set; }
        }

        public class AfricasTalkingOptions
        {
            public string apikey { get; set; }
            public string username { get; set; }
            public string senderId { get; set; }
            public string url { get; set; }
        }

        public class LocationOptions
        {
            public string apiUrl { get; set; }

            [JsonPropertyName("country_code")]
            public string CountryCode { get; set; }

            public string apiKey { get; set; }
        }

        public class WhisperSMSOptions
        {
            public string apikey { get; set; }
            public string token { get; set; }

            [JsonPropertyName("sender_id")]
            public string SenderId { get; set; }

            [JsonPropertyName("agrovesto_contact")]
            public string AgrovestoContact { get; set; }

            public string url { get; set; }
        }

        public class AppLogsOptions
        {
            public string ElasticUrls { get; set; }
            public string ElasticIndex { get; set; }
            public bool LogToFile { get; set; }
        }

        public class LoggingOptions
        {
            [JsonPropertyName("LogLevel")]
            public Dictionary<string, string> LogLevel { get; set; }
        }
    }
