using System.Text.Json.Serialization;

namespace HotelBookingApp.Models.Dtos
{
    public class AuditLogFilterDto
    {
        public int?      UserId     { get; set; }
        public string?   Action     { get; set; }
        public string?   EntityName { get; set; }
        public int?      EntityId   { get; set; }

        /// <summary>Accepts ISO date string or null. Empty string is treated as null.</summary>
        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? FromDate   { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? ToDate     { get; set; }
    }

    /// <summary>Converts empty string to null for DateTime? fields.</summary>
    public class NullableDateTimeConverter : System.Text.Json.Serialization.JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            if (reader.TokenType == System.Text.Json.JsonTokenType.Null) return null;
            if (reader.TokenType == System.Text.Json.JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s)) return null;
                if (DateTime.TryParse(s, out var dt)) return dt;
                return null;
            }
            return null;
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, DateTime? value, System.Text.Json.JsonSerializerOptions options)
        {
            if (value.HasValue) writer.WriteStringValue(value.Value.ToString("o"));
            else writer.WriteNullValue();
        }
    }
}
