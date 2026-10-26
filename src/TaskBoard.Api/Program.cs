using Scalar.AspNetCore;
using Serilog;
using TaskBoard.Api;
using TaskBoard.Api.Extensions;
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
    await app.ApplyMigrationsAsync();
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseCors(TaskBoard.Api.DependencyInjection.FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();
app.MapHub<BoardHub>("/hubs/board");

await app.RunAsync();

// Exposes Program to WebApplicationFactory in the functional tests.
public partial class Program
{
}
