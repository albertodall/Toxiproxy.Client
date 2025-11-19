# Toxiproxy.Client

A .NET library for interacting with [Shopify's Toxiproxy](https://github.com/Shopify/toxiproxy), a TCP proxy for simulating network conditions and chaos testing.

## About

Toxiproxy.Client provides a simple and intuitive .NET interface for communicating with the [Toxiproxy HTTP API](https://github.com/Shopify/toxiproxy?tab=readme-ov-file#http-api). This library enables you to test your application's resilience by simulating various network failure scenarios such as latency, timeouts, bandwidth limitations, and connection failures.

### What is Toxiproxy?

Toxiproxy is a framework specifically designed for testing, CI, and development environments that allows you to simulate network conditions deterministically. It helps you prove with tests that your application doesn't have single points of failure by introducing controlled "toxics" into your network connections.

### Key Features

- **Network Simulation**: Simulate various network conditions including latency, timeouts, bandwidth limits, and connection drops.
- **Dynamic Configuration**: Add, remove, and configure network conditions on the fly via HTTP API.
- **Testing & CI Ready**: Built specifically for automated testing environments.
- **Resilience Testing**: Verify your application can handle real-world network failures.

### Toxics

The library supports all these [toxics](https://github.com/Shopify/toxiproxy?tab=readme-ov-file#toxics).

## Installation

Install via NuGet Package Manager:

```powershell
Install-Package Toxiproxy.Client
```

Or via .NET CLI:

```bash
dotnet add package Toxiproxy.Client
```

## Usage

### Prerequisites

You need a Toxiproxy server running. Download it from the [official releases page](https://github.com/Shopify/toxiproxy/releases) or spin up a Docker container:

```bash
docker run -d -it ghcr.io/shopify/toxiproxy:latest
```

The library supports Toxiproxy server from version 2.0.0 onwards.

### Creating a client connection to the server

First of all you need to create an instance of the  `ToxiproxyClient` object, that allows the interaction with the server.
By default the client connects to the instance running on `localhost` on port `8474`.

```csharp
ToxiproxyClient client = await ToxiproxyClient.ConnectAsync();
```

otherwise, you can pass specific _hostname_ and _port_ parameters of the server you want to connect to:

```csharp
ToxiproxyClient client = await ToxiproxyClient.ConnectAsync("my-toxiproxy.domain.local", 8474);
```

### Creating a proxy on the server

Once you have a connection, you can create a _proxy_ towards another service on the network.  
This example creates a proxy in front of a SQL Server running on the same network:

```csharp
Proxy mssqlProxy = await client.ConfigureProxyAsync(cfg =>
{
    cfg.Name = "mssql_proxy";
    cfg.Listen = "127.0.0.1:11433";
    cfg.Upstream = "mssql.domain.local:1433";
});
```

It's possible to create more than one proxy on the same server; in general, you create a proxy for each service you need to test.  
This example adds a proxy for a Redis server running on the same network:

```csharp
Proxy redisProxy = await client.ConfigureProxyAsync(cfg =>
{
    cfg.Name = "redis_proxy";
    cfg.Listen = "127.0.0.1:16379";
    cfg.Upstream = "redis.domain.local:6379";
});
```

It's possible to simulate a service unavailability by disabling the service's proxy:

```csharp
await redisProxy.DisableAsync();
```

or bring it back up:

```csharp
await redisProxy.EnableAsync();
```

### Adding toxics

Once we have a proxy configured, we can add _toxics_ to is, in order to simulate connection issues.  
This example adds a _latency_ toxic to the MS SQL proxy, so to simulate a network latency while connecting to it:

```csharp
LatencyToxic latency = await mssqlProxy.AddLatencyToxicAsync(cfg => 
{ 
    cfg.Latency = 1000;
    cfg.Jitter = 10;
});
```

Here we're adding a _timeout_ toxic to the Redis proxy, so to simulate a network timeout after 1 second:

```csharp
TimeoutToxic timeout = await redisProxy.AddTimeoutToxicAsync(cfg =>
{
    cfg.Timeout = 1000;
});
```

Toxics can work both **upstream** or **downstream**; if not specified, a toxic works downstream.  
In this example we add a _bandwidth_ toxic to the MSSQL proxy so to limit the upstream bandwidth to 10 KB/s:

```csharp
BandwidthToxic bandwidth = await mssqlProxy.AddBandwidthToxicAsync(cfg =>
{
    cfg.Rate = 10;
    cfg.Stream = ToxicStream.Upstream;
});
```

### Removing toxics

Toxics on a proxy can be removed simply doing:

```csharp
await redisProxy.RemoveToxicAsync(timeout);
```

Here we remove the _timeout_ toxic from the Redis proxy.

### Reset server

If you need to reset the Toxiproxy server configuration, you can use:

```csharp
await client.ResetAsync();
```

This way, you enable all  proxies on th server and remove all active toxics.
