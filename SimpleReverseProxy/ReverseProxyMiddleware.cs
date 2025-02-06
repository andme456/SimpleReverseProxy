using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpleReverseProxy;

public class SimpleReverseProxyMiddleware
{
    private readonly RequestDelegate _nextMiddleware;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SimpleReverseProxyMiddleware> _logger;
    private readonly Dictionary<string, string> _destinationMap = new();
    private readonly Dictionary<string, string> _routeMap = new();

    public SimpleReverseProxyMiddleware(
        RequestDelegate nextMiddleware,
        IHttpClientFactory httpClientFactory,
        IOptions<ReverseProxyOptions> options, ILogger<SimpleReverseProxyMiddleware> logger)
    {
        _nextMiddleware = nextMiddleware;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var proxyOptions = options.Value;

        if (proxyOptions.Destinations.Count == 0)
        {
            return;
        }

        _destinationMap = proxyOptions.Destinations.ToDictionary(
            d => d.DestinationId,
            d => d.Url.TrimEnd('/'));

        _routeMap = proxyOptions.Sources
            .SelectMany(s => s.Routes.Select(r => new { Route = r.TrimStart('/'), s.DestinationId }))
            .ToDictionary(x => x.Route, x => x.DestinationId);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_destinationMap.Count == 0 || _routeMap.Count == 0)
        {
            await _nextMiddleware(context);
            return;
        }

        var path = context.Request.Path.Value?.TrimStart('/');
        if (string.IsNullOrEmpty(path) || !_routeMap.TryGetValue(path, out var destinationId))
        {
            await _nextMiddleware(context);
            return;
        }

        if (!_destinationMap.TryGetValue(destinationId, out var destinationUri))
        {
            await _nextMiddleware(context);
            return;
        }

        Uri targetUri = null!;

        try
        {
            CancellationTokenSource ctsTs = new(TimeSpan.FromSeconds(100));
            var cancellationToken = ctsTs.Token;
            targetUri = BuildTargetUri(context.Request, destinationUri);
            
            _logger.LogInformation("Forwarding request to {targetUri}", targetUri);

            var targetRequestMessage = await CreateTargetMessageAsync(context, targetUri, cancellationToken);

            using var httpClient = _httpClientFactory.CreateClient();

            using var responseMessage = await httpClient.SendAsync(
                targetRequestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
                
            _logger.LogInformation("Response from {targetUri}: code:{statusCode}", targetUri, responseMessage.StatusCode);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error calling target {targetUri}", targetUri);
        }
        finally
        {
            await _nextMiddleware(context);
        }
    }

    private Uri BuildTargetUri(HttpRequest request, string destinationUri)
    {
        string targetUri = $"{destinationUri}/{request.Path.Value?.TrimStart('/')}{request.QueryString}";
        return new Uri(targetUri);
    }

    private async Task<HttpRequestMessage> CreateTargetMessageAsync(HttpContext context, Uri targetUri,
        CancellationToken cancellationToken)
    {
        var requestMessage = new HttpRequestMessage();
        await CopyFromOriginalRequestContentAndHeadersAsync(context, requestMessage, cancellationToken);

        requestMessage.RequestUri = targetUri;
        requestMessage.Headers.Host = targetUri.Host;
        requestMessage.Method = GetHttpMethod(context.Request.Method);

        return requestMessage;
    }

    private async Task CopyFromOriginalRequestContentAndHeadersAsync(HttpContext context,
        HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        var requestMethod = context.Request.Method;

        if (!HttpMethods.IsGet(requestMethod) &&
            !HttpMethods.IsHead(requestMethod) &&
            !HttpMethods.IsDelete(requestMethod) &&
            !HttpMethods.IsTrace(requestMethod))
        {
            context.Request.EnableBuffering();
            var stream = context.Request.Body;
            
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            stream.Position = 0;
            
            var streamContent = new StreamContent(memoryStream);
            requestMessage.Content = streamContent;
        }

        foreach (var header in context.Request.Headers)
        {
            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    private static HttpMethod GetHttpMethod(string method)
    {
        return method switch
        {
            _ when HttpMethods.IsDelete(method) => HttpMethod.Delete,
            _ when HttpMethods.IsGet(method) => HttpMethod.Get,
            _ when HttpMethods.IsHead(method) => HttpMethod.Head,
            _ when HttpMethods.IsOptions(method) => HttpMethod.Options,
            _ when HttpMethods.IsPost(method) => HttpMethod.Post,
            _ when HttpMethods.IsPut(method) => HttpMethod.Put,
            _ when HttpMethods.IsTrace(method) => HttpMethod.Trace,
            _ when HttpMethods.IsPatch(method) => HttpMethod.Patch,
            _ => new HttpMethod(method)
        };
    }
}