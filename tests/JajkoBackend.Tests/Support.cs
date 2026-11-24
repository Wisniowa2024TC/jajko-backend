using JajkoBackend.Models;
using JajkoBackend.Services.Blich;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JajkoBackend.Tests;

public class FakeBankClient : IBankBlikClient
{
    public string? RejectWith { get; set; }
    public bool InitiateUnavailable { get; set; }
    public bool StatusUnavailable { get; set; }
    public BankPaymentState State { get; set; } = BankPaymentState.Pending;
    public int InitiateCalls { get; private set; }
    public int StatusCalls { get; private set; }
    public string? LastCode { get; private set; }
    public decimal? LastAmount { get; private set; }

    public Task<BankInitiateResult> InitiateAsync(string blikCode, decimal amount, CancellationToken ct = default)
    {
        InitiateCalls++;
        LastCode = blikCode;
        LastAmount = amount;
        if (InitiateUnavailable) throw new BankUnavailableException("down");
        return Task.FromResult(RejectWith is null
            ? new BankInitiateResult(true, Guid.NewGuid(), null)
            : new BankInitiateResult(false, null, RejectWith));
    }

    public Task<BankPaymentState> GetStatusAsync(Guid bankPaymentId, CancellationToken ct = default)
    {
        StatusCalls++;
        if (StatusUnavailable) throw new BankUnavailableException("down");
        return Task.FromResult(State);
    }
}

public class FakeTime : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
    public void Advance(TimeSpan by) => Now += by;
}

/// <summary>Service provider over one shared in-memory SQLite database, the fake bank and a fake clock.</summary>
public sealed class TestEnv : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    public ServiceProvider Services { get; }
    public FakeBankClient Bank { get; } = new();
    public FakeTime Time { get; } = new();

    public TestEnv()
    {
        _connection.Open();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddSingleton<IBankBlikClient>(Bank);
        services.AddSingleton<TimeProvider>(Time);
        services.AddSingleton<IOptions<BankOptions>>(Options.Create(new BankOptions
        {
            BaseUrl = "http://bank.test",
            MerchantId = "TEST_MERCHANT",
            PaymentTimeoutSeconds = 135
        }));
        services.AddScoped<IBlichPaymentService, BlichPaymentService>();
        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    public async Task<int> SeedOrderAsync(int userId = 1, decimal total = 45.50m)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = new Order { UserId = userId, Total = total };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    /// <summary>Runs <paramref name="action"/> in a fresh scope (fresh DbContext), like one HTTP request.</summary>
    public async Task<T> InScope<T>(Func<IBlichPaymentService, AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<IBlichPaymentService>(),
            scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public void Dispose()
    {
        Services.Dispose();
        _connection.Dispose();
    }
}
