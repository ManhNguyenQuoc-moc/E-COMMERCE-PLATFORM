using DotNetEnv;
using EcommercePlatform.Api.Endpoints;
using EcommercePlatform.Api.Extensions;
using EcommercePlatform.Application;
using EcommercePlatform.Infrastructure;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("Default");
app.UseHttpsRedirection();
app.UseApiDocumentation();

app.MapProductEndpoints();

app.Run();
