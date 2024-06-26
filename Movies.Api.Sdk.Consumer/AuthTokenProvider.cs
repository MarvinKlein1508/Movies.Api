﻿using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;

namespace Movies.Api.Sdk.Consumer;
public class AuthTokenProvider
{
    private readonly HttpClient _httpClient;
    private string _cachedToken = string.Empty;
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public AuthTokenProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetTokenAsync(CancellationToken token)
    {
        if (!string.IsNullOrEmpty(_cachedToken))
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_cachedToken);
            var expiryTimeText = jwt.Claims.Single(claim => claim.Type == "exp").Value;
            var expiryDateTime = UnixTimeStampToDateTime(int.Parse(expiryTimeText));

            if (expiryDateTime > DateTime.UtcNow)
            {
                return _cachedToken;
            }

        }

        await Lock.WaitAsync();

        var response = await _httpClient.PostAsJsonAsync("https://localhost:5003/token", new
        {
            userid = "d7dc0f07-8366-486c-b04c-4fa15c0b0cbd",
            email = "test@test.com",
            customClaims = new Dictionary<string, object>
            {
                { "admin", true },
                { "trusted_member", true }
            },
        }, token);

        var newToken = await response.Content.ReadAsStringAsync();
        _cachedToken = newToken;
        Lock.Release();
        return newToken;
    }

    private static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
    {
        var dateTime = new DateTime(1970,1,1,0,0,0,0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
}
