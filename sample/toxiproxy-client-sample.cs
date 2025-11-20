#!/usr/bin/env dotnet

#:property ManagePackageVersionsCentrally=false
#:property JsonSerializerIsReflectionEnabledByDefault=true

#:project ../src/Toxiproxy.Client/Toxiproxy.Client.csproj
#:package StackExchange.Redis@2.10.1

using StackExchange.Redis;
using Toxiproxy.Client;

Console.WriteLine("Toxiproxy.Client sample");

// Create redis proxy on Toxiproxy
ToxiproxyClient client = await ToxiproxyClient.ConnectAsync();
Toxiproxy.Client.Proxy redisProxy = await client.ConfigureProxyAsync(cfg =>
{
    cfg.Name = "redis";
    cfg.Listen = "0.0.0.0:8666";
    cfg.Upstream = "redis:6379";
});

// Connection to Redis via Toxiproxy
ConnectionMultiplexer redisProxied = await ConnectionMultiplexer.ConnectAsync("localhost:8666");
IDatabase db = redisProxied.GetDatabase();

// Set values
await db.StringSetAsync("test", "42");

// Get values
string? value = await db.StringGetAsync("test");
Console.WriteLine($"Value from proxied Redis: {value}");

// Tampering: add 5s latency
LatencyToxic latency = await redisProxy.AddLatencyToxicAsync(cfg =>
{
    cfg.Latency = 5000;
});

Console.WriteLine("Added 5s latency toxic. Getting value again...");
value = await db.StringGetAsync("test");
Console.WriteLine($"Value from proxied Redis with latency. It should have taken 5 seconds: {value}");

// Cleanup
Console.WriteLine("Cleaning up...");
await client.DeleteProxyAsync("redis");
