using System.Text.Json;
using System.Text.Json.Serialization;

namespace lotus2mqtt.LotusApi;

public class EcloudResponse<T>: EcloudResponse
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}

public class EcloudResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(CodeConverter))]
    public string Code { get; set; }


    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

public class CodeConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64().ToString();
        }
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}