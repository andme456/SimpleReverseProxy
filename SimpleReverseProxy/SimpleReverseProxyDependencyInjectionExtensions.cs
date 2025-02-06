using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleReverseProxy;

public static class SimpleReverseProxyDependencyInjectionExtensions
{
    /// <summary>
    /// Adds reverse proxy services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration section containing reverse proxy settings</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddReverseProxy(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        if (services.Any(s => s.ServiceType == typeof(IHttpClientFactory)) == false)
        {
            services.AddHttpClient();
        }
        
        services.Configure<ReverseProxyOptions>(configuration);
        return services;
    }

    /// <summary>
    /// Adds a reverse proxy middleware to the application's request pipeline
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseSimpleReverseProxy(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<SimpleReverseProxyMiddleware>();
    }
}