using DeviceManager.Application.Interfaces;
using DeviceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeviceManager.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _sqliteConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));
            services.RemoveAll(typeof(IDescriptionGenerator));

            _sqliteConnection = new SqliteConnection("Data Source=:memory:");
            _sqliteConnection.Open();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_sqliteConnection);
            });

            services.AddScoped<IDescriptionGenerator, FakeDescriptionGenerator>();

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    public override async ValueTask DisposeAsync()
    {
        if (_sqliteConnection is not null)
        {
            await _sqliteConnection.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task ExecuteDbContextAsync(Func<AppDbContext, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await operation(dbContext);
    }

    public async Task<T> ExecuteDbContextAsync<T>(Func<AppDbContext, Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await operation(dbContext);
    }

    private sealed class FakeDescriptionGenerator : IDescriptionGenerator
    {
        public Task<string> GenerateDescriptionAsync(DeviceSpecifications specs)
        {
            var description =
                $"Generated description for {specs.Name} by {specs.Manufacturer} using {specs.Processor}.";

            return Task.FromResult(description);
        }
    }
}