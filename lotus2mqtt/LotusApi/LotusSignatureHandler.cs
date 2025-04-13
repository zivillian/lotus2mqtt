using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace lotus2mqtt.LotusApi;

public class LotusSignatureHandler : DelegatingHandler
{
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var secret = "1fb43823557d423f940d8f7da9081b72";
        var nonce = Guid.NewGuid().ToString();
        var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
        var queryParameter = String.Join('&', request.RequestUri.Query
            .Replace("+", "%20")
            .Replace("*", "%2A")
            .Replace("%7E", "~")
            .Replace(",", "%2C")
            .TrimStart('?')
            .Split('&')
            .Order());
        var bodyChecksum = GetBodyChecksum(request.Content);
        var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        var method = request.Method.Method;
        var path = request.RequestUri.AbsolutePath;

        var message = $"application/json;responseformat=3\n" +
                      $"x-api-signature-nonce:{nonce}\n" +
                      $"x-api-signature-version:1.0\n" +
                      $"\n" +
                      $"{queryParameter}\n" +
                      $"{bodyChecksum}\n" +
                      $"{timestamp}\n" +
                      $"{method}\n" +
                      $"{path}";
        var hmac = HMACSHA1.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(message));

        request.Headers.Add("accept", "application/json;responseformat=3");
        FixAcceptHeader(request.Headers.Accept.First());
        request.Headers.Add("x-api-signature-nonce", nonce);
        request.Headers.Add("x-signature", Convert.ToBase64String(hmac));
        request.Headers.Add("x-timestamp", timestamp);

        return base.SendAsync(request, cancellationToken);
    }

    private readonly FieldInfo MediaTypeField = typeof(MediaTypeHeaderValue).GetField("_mediaType", BindingFlags.NonPublic | BindingFlags.Instance);
    private void FixAcceptHeader(MediaTypeWithQualityHeaderValue headersAccept)
    {
        //https://github.com/dotnet/runtime/issues/21131#issue-558185129
        //https://github.com/dotnet/runtime/issues/30171
        //https://github.com/dotnet/runtime/pull/792#issuecomment-611125494
        //but it's still broken
        headersAccept.Parameters.Clear();
        MediaTypeField.SetValue(headersAccept, "application/json;responseformat=3");
    }

    private string GetBodyChecksum(HttpContent? content)
    {
        Stream body;
        if (content is null)
        {
            body = Stream.Null;
        }
        else
        {
            body = content.ReadAsStream();
        }

        return Convert.ToBase64String(MD5.HashData(body));
    }
}