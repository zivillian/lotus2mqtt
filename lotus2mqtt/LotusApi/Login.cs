using System.Text.Json.Serialization;

namespace lotus2mqtt.LotusApi;

public class LoginRequest
{
    [JsonPropertyName("registerSource")]
    public string RegisterSource { get; set; } = "102";

    [JsonPropertyName("mobileAreaCode")]
    public string MobileAreaCode { get; set; } = "49";

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("loginType")]
    public string LoginType { get; set; } = "3";

    [JsonPropertyName("countryCode")]
    public string CountryCode { get; set; } = "DE";

    [JsonPropertyName("accountType")]
    public string AccountType { get; set; } = "2";

    [JsonPropertyName("account")]
    public string Email { get; set; }
}

public class LoginResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }
}