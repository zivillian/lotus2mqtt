using System;
using System.Text.Json;
using CommandLine;
using lotus2mqtt.LotusApi;
using Sharprompt;

namespace lotus2mqtt.Commands;

[Verb("configure", HelpText = "run config file wizard")]
public class ConfigCommand : BaseCommand
{
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        await InitAsync(cancellationToken);
        await LoginAsync(cancellationToken);
        return 0;
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
        var captcha = Prompt.Input<string>("Please paste the captcha result from https://<some-url>");
        var geetest = JsonSerializer.Deserialize<GeetestCaptchaResult>(captcha);
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

    private async Task<bool> CheckTokenAsync(CancellationToken cancellationToken)
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
}