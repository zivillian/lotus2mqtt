using System.Text.Json.Serialization;

namespace lotus2mqtt.LotusApi;

public class LotuscarsResponse<T> : LotuscarsResponse
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class LotuscarsResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("currentTime")]
    public long CurrentTime { get; set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}