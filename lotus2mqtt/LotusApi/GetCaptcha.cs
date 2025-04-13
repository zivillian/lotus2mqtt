using System.Text.Json.Serialization;

namespace lotus2mqtt.LotusApi;

public class GetCaptchaRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("captchaType")]
    public string CaptchaType { get; set; } = "2";

    [JsonPropertyName("captchaScene")]
    public string CaptchaScene { get; set; } = "101";

    [JsonPropertyName("lotNumber")]
    public string LotNumber { get; set; }

    [JsonPropertyName("captchaOutput")]
    public string CaptchaOutput { get; set; }

    [JsonPropertyName("passToken")]
    public string PassToken { get; set; }

    [JsonPropertyName("genTime")]
    public string GenTime { get; set; }

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "web-login";

}

public class GeetestCaptchaResult
{
    [JsonPropertyName("captcha_id")]
    public string CaptchaId { get; set; }

    [JsonPropertyName("lot_number")]
    public string LotNumber { get; set; }

    [JsonPropertyName("pass_token")]
    public string PassToken { get; set; }

    [JsonPropertyName("gen_time")]
    public string GenTime { get; set; }

    [JsonPropertyName("captcha_output")]
    public string CaptchaOutput { get; set; }

}