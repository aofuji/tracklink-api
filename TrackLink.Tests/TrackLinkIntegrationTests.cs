using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrackLink.Data;
using System.Text.Json;

namespace TrackLink.Tests;

public class TrackLinkIntegrationTests
{
    private static async Task<string> ReadMcpToolResultAsync(
    HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        var dataLine = content
            .Split('\n')
            .First(line => line.StartsWith("data: "));

        var jsonRpc = JsonDocument.Parse(
            dataLine["data: ".Length..]
        );

        var text = jsonRpc.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        Assert.NotNull(text);

        return text;
    }

    [Fact]
    public async Task McpGetTrackingStatus_ShouldReturnExistingTracking()
    {
        using var factory = new TrackLinkApiFactory();

        var ownerClient = await CreateAuthenticatedClientAsync(factory);

        var tracking = await CreateTrackingAsync(
            ownerClient,
            latitude: 15.5,
            longitude: 25.5
        );

        var mcpClient = factory.CreateClient();

        var request = new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new
            {
                name = "get_tracking_status",
                arguments = new
                {
                    token = tracking.Token
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/mcp"
        );

        httpRequest.Headers.Accept.ParseAdd(
            "application/json, text/event-stream"
        );

        httpRequest.Content = JsonContent.Create(request);

        var response = await mcpClient.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadMcpToolResultAsync(response);

        Assert.Contains("\"status\":\"Success\"", content);
        Assert.Contains(tracking.Token, content);
        Assert.Contains("\"latitude\":15.5", content);
        Assert.Contains("\"longitude\":25.5", content);
        Assert.DoesNotContain("\"userId\"", content);
    }

    [Fact]
    public async Task McpInitialize_ShouldReturnSuccess()
    {
        using var _factory = new TrackLinkApiFactory();
        var client = _factory.CreateClient();

        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { },
                clientInfo = new
                {
                    name = "tracklink-tests",
                    version = "1.0.0"
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/mcp"
        );

        httpRequest.Headers.Accept.ParseAdd(
            "application/json, text/event-stream"
        );

        httpRequest.Content = JsonContent.Create(request);

        var response = await client.SendAsync(httpRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"jsonrpc\":\"2.0\"", content);
        Assert.Contains("\"serverInfo\"", content);
    }

    [Fact]
    public async Task McpToolsList_ShouldExposeTrackingTools()
    {
        using var _factory = new TrackLinkApiFactory();

        var client = _factory.CreateClient();

        var request = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list",
            @params = new { }
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/mcp"
        );

        httpRequest.Headers.Accept.ParseAdd(
            "application/json, text/event-stream"
        );

        httpRequest.Content = JsonContent.Create(request);

        var response = await client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"get_tracking_status\"", content);
        Assert.Contains("\"get_tracking_history\"", content);
        Assert.Contains("\"ping\"", content);
    }

    [Fact]
    public async Task RegisteringValidUserReturnsSuccess()
    {
        using var factory = new TrackLinkApiFactory();
        var client = factory.CreateClient();

        var response = await RegisterAsync(client);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task RegisteringSameEmailTwiceReturnsConflict()
    {
        using var factory = new TrackLinkApiFactory();
        var client = factory.CreateClient();
        var email = UniqueEmail();

        var firstResponse = await RegisterAsync(client, email);
        var secondResponse = await RegisterAsync(client, email);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task LoginWithValidCredentialsReturnsAccessTokenAndRefreshToken()
    {
        using var factory = new TrackLinkApiFactory();
        var client = factory.CreateClient();
        var email = UniqueEmail();
        var password = "P@ssw0rd!";

        await RegisterAsync(client, email, password);

        var auth = await LoginAsync(client, email, password);

        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
    }

    [Fact]
    public async Task LoginWithInvalidCredentialsReturnsUnauthorized()
    {
        using var factory = new TrackLinkApiFactory();
        var client = factory.CreateClient();
        var email = UniqueEmail();

        await RegisterAsync(client, email);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password = "wrong-password"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpointWithoutAccessTokenReturnsUnauthorized()
    {
        using var factory = new TrackLinkApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tracking/my");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUserCanCreateTrackingSession()
    {
        using var factory = new TrackLinkApiFactory();
        var client = await CreateAuthenticatedClientAsync(factory);

        var tracking = await CreateTrackingAsync(client);

        Assert.False(string.IsNullOrWhiteSpace(tracking.Token));
        Assert.Equal(10.5, tracking.Latitude);
        Assert.Equal(20.5, tracking.Longitude);
    }

    [Fact]
    public async Task CreatingTrackingSessionCreatesInitialLocationHistoryEntry()
    {
        using var factory = new TrackLinkApiFactory();
        var client = await CreateAuthenticatedClientAsync(factory);

        var tracking = await CreateTrackingAsync(client, 11.1, 22.2);
        var history = await GetHistoryAsync(client, tracking.Token);

        var location = Assert.Single(history);
        Assert.Equal(11.1, location.Latitude);
        Assert.Equal(22.2, location.Longitude);
    }

    [Fact]
    public async Task GetTrackingByTokenIsPubliclyAccessibleWithoutJwt()
    {
        using var factory = new TrackLinkApiFactory();
        var ownerClient = await CreateAuthenticatedClientAsync(factory);
        var publicClient = factory.CreateClient();
        var tracking = await CreateTrackingAsync(ownerClient);

        var response = await publicClient.GetAsync($"/api/tracking/{tracking.Token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OwnerCanUpdateTheirTracking()
    {
        using var factory = new TrackLinkApiFactory();
        var client = await CreateAuthenticatedClientAsync(factory);
        var tracking = await CreateTrackingAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/tracking/{tracking.Token}",
            new
            {
                Latitude = 30.5,
                Longitude = 40.5
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<TrackingResponse>();
        Assert.NotNull(updated);
        Assert.Equal(30.5, updated.Latitude);
        Assert.Equal(40.5, updated.Longitude);
    }

    [Fact]
    public async Task AnotherAuthenticatedUserCannotUpdateSomeoneElsesTracking()
    {
        using var factory = new TrackLinkApiFactory();
        var ownerClient = await CreateAuthenticatedClientAsync(factory);
        var otherClient = await CreateAuthenticatedClientAsync(factory);
        var tracking = await CreateTrackingAsync(ownerClient);

        var response = await otherClient.PutAsJsonAsync(
            $"/api/tracking/{tracking.Token}",
            new
            {
                Latitude = 30.5,
                Longitude = 40.5
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SuccessfulUpdateCreatesNewHistoryEntry()
    {
        using var factory = new TrackLinkApiFactory();
        var client = await CreateAuthenticatedClientAsync(factory);
        var tracking = await CreateTrackingAsync(client);

        await UpdateTrackingAsync(client, tracking.Token, 30.5, 40.5);

        var history = await GetHistoryAsync(client, tracking.Token);

        Assert.Equal(2, history.Length);
        Assert.Equal(30.5, history[1].Latitude);
        Assert.Equal(40.5, history[1].Longitude);
    }

    [Fact]
    public async Task GetTrackingHistoryIsPubliclyAccessibleWithoutJwt()
    {
        using var factory = new TrackLinkApiFactory();
        var ownerClient = await CreateAuthenticatedClientAsync(factory);
        var publicClient = factory.CreateClient();
        var tracking = await CreateTrackingAsync(ownerClient);

        var response = await publicClient.GetAsync($"/api/tracking/{tracking.Token}/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LocationHistoryIsReturnedChronologically()
    {
        using var factory = new TrackLinkApiFactory();
        var client = await CreateAuthenticatedClientAsync(factory);
        var tracking = await CreateTrackingAsync(client, 1, 1);

        await Task.Delay(10);
        await UpdateTrackingAsync(client, tracking.Token, 2, 2);
        await Task.Delay(10);
        await UpdateTrackingAsync(client, tracking.Token, 3, 3);

        var history = await GetHistoryAsync(client, tracking.Token);

        Assert.Equal([1, 2, 3], history.Select(x => x.Latitude).ToArray());
        Assert.True(history[0].RecordedAt <= history[1].RecordedAt);
        Assert.True(history[1].RecordedAt <= history[2].RecordedAt);
    }

    [Fact]
    public async Task OwnerCanEndTrackingSession()
    {
        using var factory = new TrackLinkApiFactory();
        var client = await CreateAuthenticatedClientAsync(factory);
        var tracking = await CreateTrackingAsync(client);

        var response = await client.DeleteAsync($"/api/tracking/{tracking.Token}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task InactiveTrackingCannotBeUpdated()
    {
        using var factory = new TrackLinkApiFactory();
        var client = await CreateAuthenticatedClientAsync(factory);
        var tracking = await CreateTrackingAsync(client);

        var deleteResponse = await client.DeleteAsync($"/api/tracking/{tracking.Token}");
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/tracking/{tracking.Token}",
            new
            {
                Latitude = 30.5,
                Longitude = 40.5
            });

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
    }

    [Fact]
    public async Task ExpiredTrackingCannotBeUpdated()
    {
        using var factory = new TrackLinkApiFactory();
        var client = await CreateAuthenticatedClientAsync(factory);
        var tracking = await CreateTrackingAsync(client);

        await ExpireTrackingAsync(factory, tracking.Token);

        var response = await client.PutAsJsonAsync(
            $"/api/tracking/{tracking.Token}",
            new
            {
                Latitude = 30.5,
                Longitude = 40.5
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ValidRefreshTokenCanGenerateNewAccessTokenAndRefreshToken()
    {
        using var factory = new TrackLinkApiFactory();
        var client = factory.CreateClient();
        var auth = await RegisterAndLoginAsync(client);

        var rotated = await RefreshAsync(client, auth.RefreshToken);

        Assert.False(string.IsNullOrWhiteSpace(rotated.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(rotated.RefreshToken));
        Assert.NotEqual(auth.RefreshToken, rotated.RefreshToken);
    }

    [Fact]
    public async Task AfterRotationOldRefreshTokenCannotBeReused()
    {
        using var factory = new TrackLinkApiFactory();
        var client = factory.CreateClient();
        var auth = await RegisterAndLoginAsync(client);

        await RefreshAsync(client, auth.RefreshToken);

        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new
            {
                RefreshToken = auth.RefreshToken
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LogoutRevokesRefreshToken()
    {
        using var factory = new TrackLinkApiFactory();
        var client = factory.CreateClient();
        var auth = await RegisterAndLoginAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new
            {
                RefreshToken = auth.RefreshToken
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RevokedRefreshTokenCannotBeUsed()
    {
        using var factory = new TrackLinkApiFactory();
        var client = factory.CreateClient();
        var auth = await RegisterAndLoginAsync(client);

        var logoutResponse = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new
            {
                RefreshToken = auth.RefreshToken
            });
        var refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new
            {
                RefreshToken = auth.RefreshToken
            });

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    private static async Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string? email = null,
        string password = "P@ssw0rd!")
    {
        return await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Name = "Test User",
                Email = email ?? UniqueEmail(),
                Password = password
            });
    }

    private static async Task<AuthResponse> RegisterAndLoginAsync(
        HttpClient client,
        string? email = null,
        string password = "P@ssw0rd!")
    {
        email ??= UniqueEmail();

        var registerResponse = await RegisterAsync(client, email, password);
        registerResponse.EnsureSuccessStatusCode();

        return await LoginAsync(client, email, password);
    }

    private static async Task<AuthResponse> LoginAsync(
        HttpClient client,
        string email,
        string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password = password
            });

        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);

        return auth;
    }

    private static async Task<AuthResponse> RefreshAsync(
        HttpClient client,
        string refreshToken)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new
            {
                RefreshToken = refreshToken
            });

        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);

        return auth;
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(
        TrackLinkApiFactory factory)
    {
        var client = factory.CreateClient();
        var auth = await RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            auth.AccessToken);

        return client;
    }

    private static async Task<TrackingResponse> CreateTrackingAsync(
        HttpClient client,
        double latitude = 10.5,
        double longitude = 20.5)
    {
        var response = await client.PostAsJsonAsync(
            "/api/tracking",
            new
            {
                Latitude = latitude,
                Longitude = longitude
            });

        response.EnsureSuccessStatusCode();

        var tracking = await response.Content.ReadFromJsonAsync<TrackingResponse>();

        Assert.NotNull(tracking);

        return tracking;
    }

    private static async Task<TrackingResponse> UpdateTrackingAsync(
        HttpClient client,
        string token,
        double latitude,
        double longitude)
    {
        var response = await client.PutAsJsonAsync(
            $"/api/tracking/{token}",
            new
            {
                Latitude = latitude,
                Longitude = longitude
            });

        response.EnsureSuccessStatusCode();

        var tracking = await response.Content.ReadFromJsonAsync<TrackingResponse>();

        Assert.NotNull(tracking);

        return tracking;
    }

    private static async Task<TrackingLocationResponse[]> GetHistoryAsync(
        HttpClient client,
        string token)
    {
        var response = await client.GetAsync($"/api/tracking/{token}/history");

        response.EnsureSuccessStatusCode();

        var history = await response.Content.ReadFromJsonAsync<TrackingLocationResponse[]>();

        Assert.NotNull(history);

        return history;
    }

    private static async Task ExpireTrackingAsync(
        TrackLinkApiFactory factory,
        string token)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tracking = await context.Trackings.SingleAsync(x => x.Token == token);

        tracking.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        await context.SaveChangesAsync();
    }

    private static string UniqueEmail()
    {
        return $"user-{Guid.NewGuid():N}@example.com";
    }

    private sealed record AuthResponse(
        string AccessToken,
        string RefreshToken);

    private sealed record TrackingResponse(
        string Token,
        double Latitude,
        double Longitude,
        DateTime UpdatedAt,
        bool IsActive,
        DateTime ExpiresAt);

    private sealed record TrackingLocationResponse(
        double Latitude,
        double Longitude,
        DateTime RecordedAt);
}
