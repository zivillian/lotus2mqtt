using System.Text.Json.Serialization;

namespace lotus2mqtt.LotusApi;

public class SecureRequest
{
    [JsonPropertyName("authCode")]
    public string? AuthCode { get; set; }
}

public class SecureResponse
{
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
}