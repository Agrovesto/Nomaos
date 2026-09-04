using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public class EmptyStringToNullDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (string.IsNullOrWhiteSpace(str))
                    return null;

                return DateTimeOffset.Parse(str, null, System.Globalization.DateTimeStyles.RoundtripKind);
            }

            if (reader.TokenType == JsonTokenType.Null)
                return null;

            using var doc = JsonDocument.ParseValue(ref reader);
            throw new JsonException($"Unexpected token {reader.TokenType} for DateTimeOffset?.");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Value);
        }
    }
}
