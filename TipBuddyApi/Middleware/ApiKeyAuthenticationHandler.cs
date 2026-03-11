using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace TipBuddyApi.Middleware;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public const string HeaderName = "X-API-Key";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, loggerFactory, encoder)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<ApiKeyAuthenticationHandler>();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if the API key header is present
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var apiKeyHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyHeader.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Get configured API keys (supports multiple keys)
        var validApiKeys = _configuration.GetSection("ApiKeys:ValidKeys").Get<string[]>() ?? Array.Empty<string>();
        var mcpServiceUserId = _configuration["ApiKeys:McpServiceUserId"];

        if (validApiKeys.Length == 0)
        {
            _logger.LogWarning("No API keys configured in ApiKeys:ValidKeys");
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Validate the provided API key
        if (!validApiKeys.Contains(providedApiKey))
        {
            _logger.LogWarning("Invalid API key provided");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // Create claims for the authenticated MCP service
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, mcpServiceUserId ?? "mcp-service"),
            new Claim(ClaimTypes.Name, "MCP Service"),
            new Claim(ClaimTypes.Role, "McpService"),
            new Claim("auth_type", "api_key")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        _logger.LogInformation("API key authentication successful for MCP service");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
