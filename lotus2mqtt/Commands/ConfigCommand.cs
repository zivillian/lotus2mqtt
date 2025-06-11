using System;
using System.Text.Json;
using CommandLine;
using lotus2mqtt.Config;
using lotus2mqtt.LotusApi;
using lotus2mqtt.Mqtt;
using Microsoft.Extensions.Logging;
using MQTTnet.Exceptions;
using MQTTnet;
using Sharprompt;

namespace lotus2mqtt.Commands;

[Verb("configure", HelpText = "run config file wizard")]
public class ConfigCommand : BaseCommand
{
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        await InitAsync(cancellationToken);
        await LoginAsync(cancellationToken);
        await GetAuthTokenAsync(cancellationToken);
        while (!await TestMqttAsync(cancellationToken))
        {
            ConfigureMqtt();
        }
        return 0;
    }

    private void ConfigureMqtt()
    {
        Config.Mqtt.Host = Prompt.Input<string>("Please enter your mqtt server host or ip", defaultValue: Config.Mqtt.Host);

        if (!Prompt.Confirm("Does your mqtt server require credentials?"))
        {
            Config.Mqtt.Username = String.Empty;
            Config.Mqtt.Password = String.Empty;
        }
        else
        {
            Config.Mqtt.Username = Prompt.Input<string>("Please enter your mqtt username", defaultValue: Config.Mqtt.Username);
            Config.Mqtt.Password = Prompt.Password("Please enter your mqtt password");
        }

        Config.Mqtt.UseTls = Prompt.Confirm("Do you want to use TLS on port 8883?");
    }

    private async Task<bool> TestMqttAsync(CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(Config.Mqtt.Host)) return false;

        try
        {
            var factory = new MqttClientFactory(new MqttLogger(LoggerFactory));
            using var client = factory.CreateMqttClient();
            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(Config.Mqtt.Host)
                .WithTlsOptions(new MqttClientTlsOptions { UseTls = Config.Mqtt.UseTls });
            if (!String.IsNullOrEmpty(Config.Mqtt.Username) && !String.IsNullOrEmpty(Config.Mqtt.Password))
            {
                builder = builder.WithCredentials(Config.Mqtt.Username, Config.Mqtt.Password);
            }

            await client.ConnectAsync(builder.Build(), cancellationToken);
            await client.DisconnectAsync(cancellationToken: cancellationToken);
        }
        catch (MqttCommunicationException ex)
        {
            Log.LogError($"Mqtt connection failed: {ex.Message}");
            return false;
        }

        await SaveConfigAsync(cancellationToken);
        return true;
    }

    protected override async Task GetAuthTokenAsync(CancellationToken cancellationToken)
    {
        if (await CheckAccessTokenAsync(cancellationToken)) return;
        await base.GetAuthTokenAsync(cancellationToken);
    }

    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        if (await CheckTokenAsync(cancellationToken)) return;

        var email = Config.Account.Email;
        if (String.IsNullOrEmpty(email))
        {
            email = Prompt.Input<string>("Please enter your mail address");
        }

        await GetCaptchaAsync(email, cancellationToken);
        await LoginWithCodeAsync(email, cancellationToken);
    }

    private async Task GetCaptchaAsync(string email, CancellationToken cancellationToken)
    {
        GeetestCaptchaResult? geetest = null;
        while (geetest is null)
        {
            var captcha = Prompt.Input<string>("Please paste the captcha result from https://zivillian.github.io/lotus2mqtt/");
            geetest = JsonSerializer.Deserialize<GeetestCaptchaResult>(captcha);
        }
        var request = new GetCaptchaRequest
        {
            Email = email,
            CaptchaOutput = geetest.CaptchaOutput,
            GenTime = geetest.GenTime,
            LotNumber = geetest.LotNumber,
            PassToken = geetest.PassToken,
        };
        await LotusClient.GetCaptchaAsync(request, cancellationToken);
    }

    private async Task LoginWithCodeAsync(string email, CancellationToken cancellationToken)
    {
        var code = Prompt.Input<string>("Please check your mail and enter the code");
        var request = new LoginRequest
        {
            Email = email,
            Code = code
        };
        var response = await LotusClient.LoginAsync(request, cancellationToken);
        LotusClient.SetToken(response.Token);
        Config.Account.Email = response.Email;
        Config.Account.Token = response.Token;
        await SaveConfigAsync(cancellationToken);
    }
}