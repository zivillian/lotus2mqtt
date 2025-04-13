using CommandLine;
using lotus2mqtt.Commands;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    cts.Cancel();
    e.Cancel = true;
};

try
{
    return await Parser.Default.ParseArguments<ConfigCommand>(args)
        .MapResult(
            c => c.RunAsync(cts.Token),
            _ => Task.FromResult(1));
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return -1;
}