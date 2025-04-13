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
            Log.LogError("AccessToken expired - please run configure");
            return -1;
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
                    await PublishStatusAsync(car, mqtt, cancellationToken);
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

    private async Task PublishStatusAsync(string vin, IMqttClient mqtt, CancellationToken cancellationToken)
    {
        var status = await EcloudClient.GetVehicleStatus(vin, Config.Account.UserId, cancellationToken);
        throw new NotImplementedException();
    }

    private async Task<string[]> GetCarsAsync(CancellationToken cancellationToken)
    {
        var cars = await LotusClient.GetControlCars(cancellationToken);
        return cars.Select(x => x.VIN).ToArray();
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
                await client.ConnectAsync(client.Options, cancellationToken);
            }
        };

        await client.ConnectAsync(builder.Build(), cancellationToken);
        return client;
    }
}