using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace BlogApi.Integration.Extensions;

public static class HttpClientExtensions
{
    extension(HttpClient client)
    {
        public Task<HttpResponseMessage> GetWithBearerAsync([StringSyntax("Uri")] string requestUri,
            string? bearerToken, CancellationToken ct = default)
        {
            return client.SendWithBearerAsync(HttpMethod.Get, requestUri, bearerToken, ct);
        }

        private Task<HttpResponseMessage> SendWithBearerAsync(HttpMethod method,
            string requestUri, string? bearerToken, CancellationToken ct = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, requestUri);
            if (bearerToken is not null)
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, bearerToken);
            }

            return client.SendAsync(request, ct);
        }
    }
}