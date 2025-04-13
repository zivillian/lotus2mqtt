using System.Text.Json.Serialization;

namespace lotus2mqtt.LotusApi;

public class ControlCarsResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("vin")]
    public string VIN { get; set; }
}