using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace BlogApi.Integration.Extensions;

public static class HttpClientExtensions
{
    extension(HttpClient client)
    {
        public Task<HttpResponseMessage> GetWithBearerAsync([StringSyntax("Uri")] string requestUri,
            string? bearerToken, CancellationToken ct = default)
        {
            return client.SendWithBearerAsync(HttpMethod.Get, new Uri(requestUri), bearerToken, ct);
        }

        public Task<HttpResponseMessage> GetWithBearerAsync(Uri requestUri,
            string? bearerToken, CancellationToken ct = default)
        {
            return client.SendWithBearerAsync(HttpMethod.Get, requestUri, bearerToken, ct);
        }

        public Task<HttpResponseMessage> PostWithBearerAsJsonAsync<TValue>([StringSyntax("Uri")] string requestUri,
            TValue value,
            string? bearerToken,
            CancellationToken ct = default)
        {
            return client.SendWithBearerAsync(HttpMethod.Post, requestUri, value, bearerToken, ct);
        }

        private Task<HttpResponseMessage> SendWithBearerAsync(HttpMethod method,
            Uri requestUri, string? bearerToken, CancellationToken ct = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, requestUri);
            if (bearerToken is not null)
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, bearerToken);
            }

            return client.SendAsync(request, ct);
        }

        private Task<HttpResponseMessage> SendWithBearerAsync<TValue>(HttpMethod method,
            string requestUri, TValue value, string? bearerToken, CancellationToken ct = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, requestUri);
            if (bearerToken is not null)
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, bearerToken);
            }

            JsonContent content = JsonContent.Create(value);
            request.Content = content;

            return client.SendAsync(request, ct);
        }
    }
}