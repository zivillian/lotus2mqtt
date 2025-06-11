using System.Text.Json;
using System.Text.Json.Nodes;
using CommandLine;
using lotus2mqtt.Mqtt;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace lotus2mqtt.Commands;

[Verb("run", true)]
public class RunCommand : BaseCommand
{
    [Option('i', "interval", Default = 10, HelpText = "polling interval")]
    public int Intervall { get; set; }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        await InitAsync(cancellationToken);
        if (!await CheckTokenAsync(cancellationToken))
        {
            Log.LogError("Token expired - please run configure");
            return -1;
        }
        if (!await CheckAccessTokenAsync(cancellationToken))
        {
            await GetAuthTokenAsync(cancellationToken);
        }
        if (!await CheckAccessTokenAsync(cancellationToken))
        {
            Log.LogError("AccessToken expired - please run configure");
            return -2;
        }

        if (Config.Account.UserId is null)
        {
            Log.LogError("UserId missing - please run configure");
            return -3;
        }

        var cars = await GetCarsAsync(cancellationToken);
        using var mqtt = await ConnectMqttAsync(cancellationToken);

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Intervall));
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var car in cars)
                {
                    await PublishStatusAsync(car, Config.Account.UserId, mqtt, cancellationToken);
                }
                await timer.WaitForNextTickAsync(cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            //ignore
        }
        return 0;
    }

    private async Task PublishStatusAsync(string vin, string userId, IMqttClient mqtt, CancellationToken cancellationToken)
    {
        var status = await EcloudClient.GetVehicleStatusAsync(vin, userId, cancellationToken);
        await PublishAsync(mqtt, $"lotus/{vin}", status.VehicleStatus, cancellationToken);

        var soc = await EcloudClient.GetVehicleStatusSocAsync(vin, cancellationToken);
        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"lotus/{vin}/soc/soc")
            .WithPayload(soc.Soc)
            .Build();
        await mqtt.PublishAsync(message, cancellationToken);
        message = new MqttApplicationMessageBuilder()
            .WithTopic($"lotus/{vin}/soc/socTime")
            .WithPayload(soc.SocTime)
            .Build();
        await mqtt.PublishAsync(message, cancellationToken);
    }

    private async Task PublishAsync(IMqttClient mqtt, string topic, JsonNode? node, CancellationToken cancellationToken)
    {
        string? payload = null;
        if (node is not null)
        {
            switch (node.GetValueKind())
            {
                case JsonValueKind.Object:
                    var json = node.AsObject();
                    foreach (var property in json)
                    {
                        await PublishAsync(mqtt, $"{topic}/{property.Key}", property.Value, cancellationToken);
                    }

                    return;
                case JsonValueKind.String:
                    payload = node.GetValue<string>();
                    break;
                //case JsonValueKind.Number:
                //    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    payload = node.GetValue<bool>().ToString().ToLower();
                    break;
                case JsonValueKind.Null:
                    payload = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build();
        await mqtt.PublishAsync(message, cancellationToken);
    }

    private async Task<string[]> GetCarsAsync(CancellationToken cancellationToken)
    {
        var cars = await LotusClient.GetControlCars(cancellationToken);
        return cars.Select(x => x.VIN).Where(x => x is not null).OfType<string>().ToArray();
    }

    private async Task<IMqttClient> ConnectMqttAsync(CancellationToken cancellationToken)
    {
        var factory = new MqttClientFactory(new MqttLogger(LoggerFactory));
        var client = factory.CreateMqttClient();
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(Config.Mqtt.Host)
            .WithTlsOptions(new MqttClientTlsOptions { UseTls = Config.Mqtt.UseTls });
        if (!String.IsNullOrEmpty(Config.Mqtt.Username) && !String.IsNullOrEmpty(Config.Mqtt.Password))
        {
            builder = builder.WithCredentials(Config.Mqtt.Username, Config.Mqtt.Password);
        }

        client.DisconnectedAsync += async e =>
        {
            if (e.ClientWasConnected)
            {
                if (cancellationToken.IsCancellationRequested) return;
                await client.ConnectAsync(client.Options, cancellationToken);
            }
        };

        await client.ConnectAsync(builder.Build(), cancellationToken);
        return client;
    }
}