using System.Security.Cryptography.X509Certificates;
using CommandLine;
using lotus2mqtt.Config;
using lotus2mqtt.LotusApi;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using Factory= Microsoft.Extensions.Logging.LoggerFactory;

namespace lotus2mqtt.Commands;

public abstract class BaseCommand
{
    [Option('c', "config", Default = "lotus2mqtt.yml", HelpText = "path to yaml config file")]
    public string ConfigFile { get; set; }

    [Option('d', "debug", Default = false, HelpText = "enable debug logging")]
    public bool Debug { get; set; }

    protected ILoggerFactory LoggerFactory { get; private set; }

    protected ILogger Log { get; private set; }

    protected LotusConfig Config { get; private set; }

    protected LotuscarsClient LotusClient { get; private set; }

    protected EcloudClient EcloudClient { get; private set; }

    protected async Task InitAsync(CancellationToken cancellationToken)
    {
        LoggerFactory = Factory.Create(x => x.SetMinimumLevel(Debug ? LogLevel.Trace : LogLevel.Error).AddConsole());
        Log = LoggerFactory.CreateLogger(this.GetType());
        var file = new FileInfo(ConfigFile);
        if (file.Exists)
        {
            var deserializer = new Deserializer();
            using var content = file.OpenText();
            Config = deserializer.Deserialize<LotusConfig>(content);
        }
        else
        {
            Config = new LotusConfig();
            await SaveConfigAsync(cancellationToken);
        }

        var httpLogger = LoggerFactory.CreateLogger<HttpClient>();
        var lotusHttpOptions = new HttpClientFactoryOptions
        {
            ShouldRedactHeaderValue = x => "token".Equals(x, StringComparison.InvariantCultureIgnoreCase),
        };
        var httpClient = new HttpClient(new LoggingHttpMessageHandler(httpLogger, lotusHttpOptions)
        {
            InnerHandler = new HttpClientHandler()
        });
        LotusClient = new LotuscarsClient(httpClient);
        var certHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = EcloudClient.ValidateCertificate,
            ClientCertificates =
            {
                EcloudClient.ClientCert
            }
        };
        var ecloudHttpOptions = new HttpClientFactoryOptions
        {
            ShouldRedactHeaderValue = x => "authorization".Equals(x, StringComparison.InvariantCultureIgnoreCase),
        };
        EcloudClient = new EcloudClient(new HttpClient(new LotusSignatureHandler
        {
            InnerHandler = new LoggingHttpMessageHandler(httpLogger, ecloudHttpOptions)
            {
                InnerHandler = certHandler
            }
        }));
    }

    protected async Task SaveConfigAsync(CancellationToken cancellationToken)
    {
        var serializer = new Serializer();
        await using var configFile = File.OpenWrite(ConfigFile);
        configFile.SetLength(0);
        await using var writer = new StreamWriter(configFile);
        serializer.Serialize(writer, Config);
        await writer.FlushAsync(cancellationToken);
        await configFile.FlushAsync(cancellationToken);
    }

    protected async Task<bool> CheckTokenAsync(CancellationToken cancellationToken)
    {
        LotusClient.SetToken(Config.Account.Token);
        try
        {
            await LotusClient.InfoAsync(cancellationToken);
            return true;
        }
        catch (LotusHttpException)
        {
            LotusClient.SetToken(null);
            return false;
        }
    }

    protected async Task<bool> CheckAccessTokenAsync(CancellationToken cancellationToken)
    {
        EcloudClient.SetToken(Config.Account.AccessToken);
        try
        {
            await EcloudClient.GetVehicleState("X", "0", cancellationToken);
            return true;
        }
        catch (EcloudHttpException ex)
        {
            if (ex.Response.Code == "1402")
            {
                LotusClient.SetToken(null);
                return false;
            }

            return true;
        }
    }
}