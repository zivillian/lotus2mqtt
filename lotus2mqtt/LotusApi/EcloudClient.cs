using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace lotus2mqtt.LotusApi;

public class EcloudClient
{
    private readonly HttpClient _client;

    public static X509Certificate2 ClientCert { get; } = new (X509Certificate2.CreateFromPem(
        """
        -----BEGIN CERTIFICATE-----
        MIIDgjCCAymgAwIBAgIQLfDnI3kEdUYNhWRP98hXuzAKBggqhkjOPQQDAjCBjjEL
        MAkGA1UEBhMCQ04xDjAMBgNVBAgMBUh1YmVpMQ4wDAYDVQQHDAVXdWhhbjETMBEG
        A1UECgwKTG90dXMgVGVjaDEmMCQGA1UECwwdTG90dXMgS2V5IE1hbmFnZW1lbnQg
        UGxhdGZvcm0xIjAgBgNVBAMMGU1vYmlsZSBJc3N1aW5nIFByb2QgQ0EgQ04wHhcN
        MjMxMDExMDExOTAyWhcNMjgxMDA5MDExOTAyWjCCAQ0xCzAJBgNVBAYTAkNOMRUw
        EwYKCZImiZPyLGQBGRYFbG90dXMxHTAbBgoJkiaJk/IsZAEZFg1Db25uZWN0ZWQg
        Q2FyMQ4wDAYDVQQIDAVIdWJlaTEOMAwGA1UEBwwFd3VoYW4xFDASBgNVBAoMCzEy
        MTJ0ZXN0NTU1MRkwFwYDVQQLDBA4NjY1NzcwNDEzNjA0NjIxMS4wLAYDVQQDDCVt
        ZS1pY3NwLWFwcC1zdGFnZS5sb3R1c2NhcnMubGluazo4MjgyMUcwRQYKCZImiZPy
        LGQBAQw3MTAwMDU2NTY1NjU2OTY5NjkxMTExMV82MDFhOTQ2ODY2OTI0ZTYxYmFk
        OTNmMWZmYTIyY2IzNDBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABMKhr+3Qloer
        bLn9WkZVs5+AlQXOJi0Jk4IpET36rCwdnEKN8r22Q5wxLlp3YrcK2vMhblB7pyjt
        9ohJ6/5fdzWjgeYwgeMwHwYDVR0jBBgwFoAUQ8xYyIzxtieLTU68j6mvrRRKn7ww
        HQYDVR0OBBYEFETLwEDpRg3lSLZCVhY5bjMQo0SbMD0GCCsGAQUFBwEBBDEwLzAt
        BggrBgEFBQcwAYYhaHR0cDovL29jc3AubG90dXNjYXJzLmNvbS5jbjo5NDQ0MAsG
        A1UdDwQEAwIDqDAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwEQYDUQEB
        BAoMCDEyMzQ1Njc4MBAGA1EBAgQJDAdrZTExMTExMBEGA1EBAwQKDAhrZXkyMjIy
        MjAKBggqhkjOPQQDAgNHADBEAiBArAzu7vuDrnXhSP1k3lEWrdhfzrc3pA3tLbRp
        rtHmlQIgT4usN7cRWliNRbCOMrjzq8W60mxji6n3zrvVBA7mG1w=
        -----END CERTIFICATE-----
        """,
        """
        -----BEGIN PRIVATE KEY-----
        MIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQgj1C1fDEQ9gyoDXYN
        R2FRIJNPz4NvaEXaX16tBkIJsLegCgYIKoZIzj0DAQehRANCAATCoa/t0JaHq2y5
        /VpGVbOfgJUFziYtCZOCKRE9+qwsHZxCjfK9tkOcMS5ad2K3CtrzIW5Qe6co7faI
        Sev+X3c1
        -----END PRIVATE KEY-----
        """)
        .Export(X509ContentType.Pkcs12));

    public EcloudClient(HttpClient client)
    {
        _client = client;
        _client.BaseAddress = new Uri("https://apis-l.ecloudeu.com/");
        _client.DefaultRequestHeaders.Add("x-app-id", "com.geely.lotusInternational");
        _client.DefaultRequestHeaders.Add("x-api-signature-version", "1.0");
        _client.DefaultRequestHeaders.Add("x-operator-code", "LOTUS");
    }

    public void SetToken(string? token)
    {
        if (_client.DefaultRequestHeaders.Contains("Authorization"))
        {
            _client.DefaultRequestHeaders.Remove("Authorization");
        }
        if (String.IsNullOrEmpty(token))
        {
            return;
        }

        _client.DefaultRequestHeaders.Add("Authorization", token);
    }

    public static bool ValidateCertificate(HttpRequestMessage request, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors)
    {
        return chain is not null && chain.ChainElements.Any(x => x.Certificate.Thumbprint.Equals("07b7743c77f9cfe3369edc23e1e44be9e6af8987", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<SecureResponse> SecureAsync(string authCode, CancellationToken cancellationToken)
    {
        var request = new SecureRequest { AuthCode = authCode };
        var response = await _client.PostAsJsonAsync("auth/account/session/secure?identity_type=lotus", request, cancellationToken);
        return await GetResponseAsync<SecureResponse>(response, cancellationToken);
    }

    public async Task<VehicleStatusResponse> GetVehicleStatusAsync(string vin, string userId, CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync($"remote-control/vehicle/status/{vin}?userId={userId}&latest=true", cancellationToken);
        return await GetResponseAsync<VehicleStatusResponse>(response, cancellationToken);
    }

    public async Task<VehicleStatusSocResponse> GetVehicleStatusSocAsync(string vin, CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync($"remote-control/vehicle/status/soc/{vin}?setting=charging", cancellationToken);
        return await GetResponseAsync<VehicleStatusSocResponse>(response, cancellationToken);
    }

    public async Task GetVehicleState(string vin, string userId, CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync($"remote-control/vehicle/status/state/{vin}?userId={userId}", cancellationToken);
        await CheckResponseAsync(response, cancellationToken);
    }

    private async Task CheckResponseAsync(HttpResponseMessage message, CancellationToken cancellationToken)
    {
        message.EnsureSuccessStatusCode();
        var response = await message.Content.ReadFromJsonAsync<EcloudResponse>(cancellationToken);
        if (response?.Success != true)
        {
            throw new EcloudHttpException(response);
        }
    }

    private async Task<T> GetResponseAsync<T>(HttpResponseMessage message, CancellationToken cancellationToken)
    {
        message.EnsureSuccessStatusCode();
        var response = await message.Content.ReadFromJsonAsync<EcloudResponse<T>>(cancellationToken);
        if (response?.Success != true)
        {
            throw new EcloudHttpException(response);
        }

        if (response.Data is null)
            throw new NullReferenceException("data of response is null");
        return response.Data;
    }
}