using System;
using System.Net.Http.Json;

namespace lotus2mqtt.LotusApi;

public class LotuscarsClient
{
    private readonly HttpClient _client;

    public LotuscarsClient(HttpClient client)
    {
        _client = client;
        _client.BaseAddress = new Uri("https://access-app-global.lotuscars.link/cidpsso/");
    }

    public async Task GetCaptchaAsync(GetCaptchaRequest request, CancellationToken cancellationToken)
    {
        var response = await _client.PostAsJsonAsync("captcha/v3/getCaptcha", request, cancellationToken);
        await CheckResponseAsync(response, cancellationToken);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _client.PostAsJsonAsync("user/v3/login", request, cancellationToken);
        return await GetResponseAsync<LoginResponse>(response, cancellationToken);
    }

    public async Task InfoAsync(CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync("user/v3/info", cancellationToken);
        await CheckResponseAsync(response, cancellationToken);
    }

    public void SetToken(string? token)
    {
        if (String.IsNullOrEmpty(token))
        {
            _client.DefaultRequestHeaders.Remove("token");
        }
        else
        {
            _client.DefaultRequestHeaders.Add("token", token);
        }
    }

    public async Task<GetCodeResponse> GetCodeAsync(CancellationToken cancellationToken)
    {
        var request = new GetCodeRequest { State = Guid.NewGuid().ToString() };
        var response = await _client.PostAsJsonAsync("oauth2/v1/getCode", request, cancellationToken);
        return await GetResponseAsync<GetCodeResponse>(response, cancellationToken);
    }

    private async Task CheckResponseAsync(HttpResponseMessage message, CancellationToken cancellationToken)
    {
        message.EnsureSuccessStatusCode();
        var response = await message.Content.ReadFromJsonAsync<LotuscarsResponse>(cancellationToken);
        if (!response.Success)
        {
            throw new LotusHttpException(response);
        }
    }

    private async Task<T> GetResponseAsync<T>(HttpResponseMessage message, CancellationToken cancellationToken)
    {
        message.EnsureSuccessStatusCode();
        var response = await message.Content.ReadFromJsonAsync<LotuscarsResponse<T>>(cancellationToken);
        if (!response.Success)
        {
            throw new LotusHttpException(response);
        }
        return response.Data;
    }
}