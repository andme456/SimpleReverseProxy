# Simple Reverse Proxy

A lightweight reverse proxy middleware for ASP.NET Core applications that allows forwarding requests to different destinations based on route configurations.

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package SimpleReverseProxy
```

## Usage

1. Add the reverse proxy services in your `Program.cs` or `Startup.cs`:

```csharp
services.AddReverseProxy(Configuration.GetSection("ReverseProxy"));
```

2. Add the middleware to your application pipeline:

```csharp
app.UseSimpleReverseProxy();
```

3. Configure the reverse proxy in your `appsettings.json`:

```json
{
  "ReverseProxy": {
      "Sources": [
      {
        "DestinationId": "api1",
        "Routes": [ "users", "products" ]
      },
      {
        "DestinationId": "api2",
        "Routes": [ "orders", "payments" ]
      }
    ],
    "Destinations": [
      {
        "DestinationId": "api1",
        "Url": "https://api1.example.com"
      },
      {
        "DestinationId": "api2",
        "Url": "https://api2.example.com"
      }
    ]
  }
}
```

With this configuration:
- Requests to `/users` and `/products` will be forwarded to `https://api1.example.com`
- Requests to `/orders` and `/payments` will be forwarded to `https://api2.example.com`

## License

MIT
