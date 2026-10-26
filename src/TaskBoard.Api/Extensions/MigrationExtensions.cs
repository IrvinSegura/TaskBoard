using Microsoft.EntityFrameworkCore;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Api.Extensions;

public static class MigrationExtensions
{
    // Development only. In production, run migrations as a deployment step.
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
}
