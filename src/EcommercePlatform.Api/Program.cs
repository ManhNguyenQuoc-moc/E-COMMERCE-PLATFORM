using DotNetEnv;
using EcommercePlatform.Api.Endpoints;
using EcommercePlatform.Api.Extensions;
using EcommercePlatform.Api.Grpc;
using EcommercePlatform.Api.Middlewares;
using EcommercePlatform.Application;
using EcommercePlatform.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using EcommercePlatform.Api.Hubs;
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var grpcPort = builder.Configuration.GetValue<int?>("Grpc:Port");
if (grpcPort is > 0)
{
    builder.WebHost.ConfigureKestrel((context, options) =>
    {
        var urls = context.Configuration["ASPNETCORE_URLS"]?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (urls.Length == 0)
        {
            options.ListenAnyIP(5185, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
        }
        else
        {
            foreach (var url in urls)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    continue;
                }

                if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    options.ListenAnyIP(uri.Port, listenOptions =>
                    {
                        listenOptions.UseHttps();
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });
                }
                else
                {
                    options.ListenAnyIP(uri.Port, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
                }
            }
        }

        options.ListenAnyIP(grpcPort.Value, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });
    });
}

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseExceptionHandler();
app.UseCors("Default");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseMiddleware<SecurityMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();

app.UseApiDocumentation();

app.MapAuthEndpoints();
app.MapProductEndpoints();
app.MapCartEndpoints();
app.MapOrderEndpoints();
app.MapAdminEndpoints();
app.MapReviewEndpoints();
app.MapGrpcService<CatalogGrpcService>();
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();
