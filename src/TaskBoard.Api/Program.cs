using Scalar.AspNetCore;
using Serilog;
using TaskBoard.Api;
using TaskBoard.Api.Extensions;
using TaskBoard.Api.HealthChecks;
using TaskBoard.Api.Realtime;
using TaskBoard.Application;
using TaskBoard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration));

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Everywhere else migrations run as a deployment step (the "migrations" image), never at startup.
    await app.ApplyMigrationsAsync();
}

// First, so logging and the rate limiter see the client address instead of the proxy's.
app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseCors(TaskBoard.Api.DependencyInjection.FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapEndpoints();
app.MapHub<BoardHub>("/hubs/board");

await app.RunAsync();

// Exposes Program to WebApplicationFactory in the functional tests.
public partial class Program
{
}
