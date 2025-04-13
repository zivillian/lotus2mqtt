using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace lotus2mqtt.LotusApi;

public class VehicleStatusResponse
{
    [JsonPropertyName("vehicleStatus")]
    public JsonObject VehicleStatus { get; set; }
}