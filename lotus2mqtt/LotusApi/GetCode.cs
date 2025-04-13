using System.Text.Json.Serialization;

namespace lotus2mqtt.LotusApi;

public class GetCodeRequest
{
    [JsonPropertyName("state")]
    public string? State { get; set; }
}

public class GetCodeResponse
{
    [JsonPropertyName("accessCode")]
    public string? AccessCode { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }
}