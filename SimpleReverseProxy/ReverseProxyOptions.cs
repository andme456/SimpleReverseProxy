// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
namespace SimpleReverseProxy;

public class ReverseProxyOptions
{
    public List<ProxyDestination> Destinations { get; set; } = [];
    public List<ProxyRoute> Sources { get; set; } = [];
}

public class ProxyDestination
{
    public string DestinationId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class ProxyRoute
{
    public string DestinationId { get; set; } = string.Empty;
    public List<string> Routes { get; set; } = [];
}