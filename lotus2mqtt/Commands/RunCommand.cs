using CommandLine;
using Microsoft.Extensions.Logging;

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

        var cars = await GetCarsAsync(cancellationToken);
        return 0;
    }

    private async Task<string[]> GetCarsAsync(CancellationToken cancellationToken)
    {
        var cars = await LotusClient.GetControlCars(cancellationToken);
        return cars.Select(x => x.VIN).ToArray();
    }
}