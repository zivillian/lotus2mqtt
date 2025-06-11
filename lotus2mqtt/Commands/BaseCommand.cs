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
    public string ConfigFile { get; set; } = "lotus2mqtt.yml";

    [Option('d', "debug", Default = false, HelpText = "enable debug logging")]
    public bool Debug { get; set; }

    private ILoggerFactory? _loggerFactory;
    protected ILoggerFactory LoggerFactory { 
        get
        {
            if (_loggerFactory is null)
            {
                _loggerFactory = Factory.Create(x => x.SetMinimumLevel(Debug ? LogLevel.Trace : LogLevel.Error).AddConsole());
            }
            return _loggerFactory;
        }
    }

    private ILogger? _log;
    protected ILogger Log 
    { 
        get
        {
            if (_log is null)
            {
                _log = LoggerFactory.CreateLogger(GetType());
            }
            return _log;
        }
    }

    protected LotusConfig Config { get; private set; } = new();

    private LotuscarsClient? _lotusClient;
    protected LotuscarsClient LotusClient 
    {
        get
        {
            if (_lotusClient is null)
            {
                var httpLogger = LoggerFactory.CreateLogger<HttpClient>();
                var lotusHttpOptions = new HttpClientFactoryOptions
                {
                    ShouldRedactHeaderValue = x => "token".Equals(x, StringComparison.InvariantCultureIgnoreCase),
                };
                var httpClient = new HttpClient(new LoggingHttpMessageHandler(httpLogger, lotusHttpOptions)
                {
                    InnerHandler = new HttpClientHandler()
                });
                _lotusClient = new LotuscarsClient(httpClient);
            }
            return _lotusClient;
        }
    }

    private EcloudClient? _ecloudClient;
    protected EcloudClient EcloudClient 
    { 
        get
        {
            if (_ecloudClient is null)
            {
                var httpLogger = LoggerFactory.CreateLogger<HttpClient>();
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
                _ecloudClient = new EcloudClient(new HttpClient(new LotusSignatureHandler
                {
                    InnerHandler = new LoggingHttpMessageHandler(httpLogger, ecloudHttpOptions)
                    {
                        InnerHandler = certHandler
                    }
                }));

            }
            return _ecloudClient;
        }
    }

    protected async Task InitAsync(CancellationToken cancellationToken)
    {
        var file = new FileInfo(ConfigFile);
        if (file.Exists)
        {
            var deserializer = new Deserializer();
            using var content = file.OpenText();
            Config = deserializer.Deserialize<LotusConfig>(content);
        }
        else
        {
            await SaveConfigAsync(cancellationToken);
        }
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
            if (ex.Response?.Code == "1402")
            {
                EcloudClient.SetToken(null);
                return false;
            }

            return true;
        }
    }

    protected virtual async Task GetAuthTokenAsync(CancellationToken cancellationToken)
    {
        var response = await LotusClient.GetCodeAsync(cancellationToken);
        if (String.IsNullOrEmpty(response.AccessCode))
            throw new ArgumentNullException(message: "no access code returned", null);
        var tokens = await EcloudClient.SecureAsync(response.AccessCode, cancellationToken);
        Config.Account.AccessToken = tokens.AccessToken;
        Config.Account.RefreshToken = tokens.RefreshToken;
        Config.Account.UserId = tokens.UserId;
        await SaveConfigAsync(cancellationToken);
    }
}