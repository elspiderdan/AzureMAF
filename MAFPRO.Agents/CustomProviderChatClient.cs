using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace MAFPRO.Agents;

public class CustomProviderChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _deploymentId;
    private readonly string _apiVersion;
    private readonly string _subscriptionKey;
    private readonly string _aadToken;
    private readonly string _sourceIp;
    private readonly string _userId;

    public CustomProviderChatClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["CustomProviderAI:BaseUrl"] ?? string.Empty;
        _deploymentId = configuration["CustomProviderAI:DeploymentId"] ?? string.Empty;
        _apiVersion = configuration["CustomProviderAI:ApiVersion"] ?? "2024-10-21";
        _subscriptionKey = configuration["CustomProviderAI:SubscriptionKey"] ?? string.Empty;
        _aadToken = configuration["CustomProviderAI:AadToken"] ?? string.Empty;
        _sourceIp = configuration["CustomProviderAI:SourceIp"] ?? "127.0.0.1";
        _userId = configuration["CustomProviderAI:UserId"] ?? "user@local";
    }

    public ChatClientMetadata Metadata =>
        new("CustomApimProvider", new Uri(string.IsNullOrWhiteSpace(_baseUrl) ? "http://localhost" : _baseUrl), _deploymentId);

    public void Dispose()
    {
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(_deploymentId) || string.IsNullOrWhiteSpace(_subscriptionKey))
        {
            throw new InvalidOperationException("Custom provider is not fully configured (BaseUrl/DeploymentId/SubscriptionKey).");
        }

        var requestUrl = $"{_baseUrl.TrimEnd('/')}/aoai/openai/deployments/{_deploymentId}/chat/completions?api-version={_apiVersion}";
        var body = new
        {
            messages = chatMessages.Select(m => new
            {
                role = m.Role == ChatRole.Assistant ? "assistant" : m.Role == ChatRole.System ? "system" : "user",
                content = m.Text ?? string.Empty
            })
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.TryAddWithoutValidation("ocp-apim-subscription-key", _subscriptionKey);
        request.Headers.TryAddWithoutValidation("ocp-apim-trace", "TRUE");
        request.Headers.TryAddWithoutValidation("SourceIP", _sourceIp);
        request.Headers.TryAddWithoutValidation("UserID", _userId);
        if (!string.IsNullOrWhiteSpace(_aadToken))
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_aadToken}");
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Custom provider error {(int)response.StatusCode}: {payload}");
        }

        using var document = JsonDocument.Parse(payload);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, content));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
