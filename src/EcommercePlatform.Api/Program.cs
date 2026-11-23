using DotNetEnv;
using EcommercePlatform.Api.Endpoints;
using EcommercePlatform.Api.Extensions;
using EcommercePlatform.Api.Middlewares;
using EcommercePlatform.Application;
using EcommercePlatform.Infrastructure;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("Default");
app.UseHttpsRedirection();

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

app.Run();
