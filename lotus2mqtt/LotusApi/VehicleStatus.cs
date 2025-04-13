using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace lotus2mqtt.LotusApi;

public class VehicleStatusResponse
{
    [JsonPropertyName("vehicleStatus")]
    public JsonObject? VehicleStatus { get; set; }
}

public class VehicleStatusSocResponse
{
    [JsonPropertyName("socTime")]
    public string? SocTime { get; set; }

    [JsonPropertyName("soc")]
    public string? Soc { get; set; }
}