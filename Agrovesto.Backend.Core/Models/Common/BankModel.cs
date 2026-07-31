using System.Text.Json.Serialization;


namespace Agrovesto.Backend.Core.Models.Common
{
    public class BankModel
    {
        [JsonPropertyName("Code")]
        public string BankCode { get; set; } = string.Empty;

        [JsonPropertyName("Bank_name")]
        public string BankName { get; set; } = string.Empty;
    }
}
