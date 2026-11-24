using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JajkoBackend.Models;
using JajkoBackend.Services.Blich;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace JajkoBackend.Tests;

public sealed class BlichApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    public FakeBankClient Bank { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

            services.RemoveAll<IBankBlikClient>();
            services.AddSingleton<IBankBlikClient>(Bank);

            // The reconciler would race the tests on the shared connection; it has its own tests.
            services.RemoveAll<IHostedService>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

public class BlichApiTests(BlichApiFactory factory) : IClassFixture<BlichApiFactory>
{
    private const string Password = "Passw0rd!x";

    private async Task<(HttpClient Client, int UserId)> LoginAsNewUserAsync()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var client = factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = Password, name = "Jan", surname = "Kowalski" });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginReply>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email);
        return (client, user!.Id);
    }

    private async Task<int> SeedOrderAsync(int userId, decimal total = 45.50m)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = new Order { UserId = userId, Total = total };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    private static Task<HttpResponseMessage> Pay(HttpClient c, int orderId, string code) =>
        c.PostAsJsonAsync($"/api/orders/{orderId}/blich-payments", new { blikCode = code });

    private record LoginReply(string AccessToken);
    private record PaymentReply(Guid PaymentId, int OrderId, string Status, decimal Amount, bool OrderPaid);

    [Fact]
    public async Task Start_WithoutToken_IsUnauthorized()
    {
        var response = await Pay(factory.CreateClient(), 1, "123456");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12a456")]
    [InlineData("")]
    public async Task Start_WithMalformedCode_IsBadRequest_AndNeverCallsBank(string code)
    {
        var (client, userId) = await LoginAsNewUserAsync();
        var orderId = await SeedOrderAsync(userId);
        var calls = factory.Bank.InitiateCalls;

        var response = await Pay(client, orderId, code);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(calls, factory.Bank.InitiateCalls);
    }

    [Fact]
    public async Task Start_ForAnotherUsersOrder_IsNotFound()
    {
        var (_, ownerId) = await LoginAsNewUserAsync();
        var orderId = await SeedOrderAsync(ownerId);
        var (stranger, _) = await LoginAsNewUserAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await Pay(stranger, orderId, "123456")).StatusCode);
    }

    [Fact]
    public async Task HappyPath_Start_Poll_Approve_MarksOrderPaid()
    {
        var (client, userId) = await LoginAsNewUserAsync();
        var orderId = await SeedOrderAsync(userId, total: 45.50m);
        factory.Bank.State = BankPaymentState.Pending;

        var start = await Pay(client, orderId, "481920");

        Assert.Equal(HttpStatusCode.Accepted, start.StatusCode);
        var started = (await start.Content.ReadFromJsonAsync<PaymentReply>())!;
        Assert.Equal("Pending", started.Status);
        Assert.Equal(45.50m, started.Amount);
        Assert.EndsWith($"/api/blich-payments/{started.PaymentId}", start.Headers.Location!.AbsolutePath);

        var pending = await client.GetFromJsonAsync<PaymentReply>($"/api/blich-payments/{started.PaymentId}");
        Assert.Equal("Pending", pending!.Status);

        factory.Bank.State = BankPaymentState.Approved;
        await Task.Delay(1100); // the endpoint throttles bank calls to one per second
        var approved = await client.GetFromJsonAsync<PaymentReply>($"/api/blich-payments/{started.PaymentId}");
        Assert.Equal("Approved", approved!.Status);
        Assert.True(approved.OrderPaid);

        // A second attempt on a paid order is a conflict.
        Assert.Equal(HttpStatusCode.Conflict, (await Pay(client, orderId, "654321")).StatusCode);

        var history = await client.GetFromJsonAsync<List<PaymentReply>>($"/api/orders/{orderId}/blich-payments");
        Assert.Single(history!);
    }

    [Fact]
    public async Task Start_WhenBankRejectsCode_ReturnsBankMessage()
    {
        var (client, userId) = await LoginAsNewUserAsync();
        var orderId = await SeedOrderAsync(userId);
        factory.Bank.RejectWith = "Invalid or expired BLIH code.";
        try
        {
            var response = await Pay(client, orderId, "000000");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Invalid or expired BLIH code.", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            factory.Bank.RejectWith = null;
        }
    }

    [Fact]
    public async Task Start_WhenBankIsDown_Returns502()
    {
        var (client, userId) = await LoginAsNewUserAsync();
        var orderId = await SeedOrderAsync(userId);
        factory.Bank.InitiateUnavailable = true;
        try
        {
            Assert.Equal(HttpStatusCode.BadGateway, (await Pay(client, orderId, "123456")).StatusCode);
        }
        finally
        {
            factory.Bank.InitiateUnavailable = false;
        }
    }

    [Fact]
    public async Task Get_AnotherUsersPayment_IsNotFound()
    {
        var (owner, ownerId) = await LoginAsNewUserAsync();
        var orderId = await SeedOrderAsync(ownerId);
        var started = (await (await Pay(owner, orderId, "123456")).Content.ReadFromJsonAsync<PaymentReply>())!;
        var (stranger, _) = await LoginAsNewUserAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/blich-payments/{started.PaymentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/orders/{orderId}/blich-payments")).StatusCode);
    }

    [Fact]
    public async Task Start_IsRateLimited_PerUser()
    {
        var (client, userId) = await LoginAsNewUserAsync();
        var orderId = await SeedOrderAsync(userId);

        HttpStatusCode last = default;
        for (var i = 0; i < 6; i++)
        {
            last = (await Pay(client, orderId, "12345")).StatusCode; // invalid code: cheap, never reaches the bank
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last);
    }

    [Fact]
    public async Task CreateOrder_StoresTotal_AndLinksItemsToTheOrder()
    {
        var (client, userId) = await LoginAsNewUserAsync();
        int itemId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = new Category { Name = "Eggs" };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            var item = new Item { CategoryId = category.Id, Name = "Egg", Price = 2.50m, ReferencePrice = 2.50m };
            db.Items.Add(item);
            await db.SaveChangesAsync();
            db.ItemStocks.Add(new ItemStock { ItemId = item.Id, Amount = 100 });
            db.CartItems.Add(new CartItem { UserId = userId, ItemId = item.Id, Quantity = 4 });
            await db.SaveChangesAsync();
            itemId = item.Id;
        }

        var response = await client.PostAsync("/api/orders/order", null);
        response.EnsureSuccessStatusCode();
        var orderId = await response.Content.ReadFromJsonAsync<int>();

        using var check = factory.Services.CreateScope();
        var checkDb = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotEqual(0, orderId);
        Assert.Equal(10.00m, (await checkDb.Orders.AsNoTracking().SingleAsync(o => o.Id == orderId)).Total);
        Assert.Equal(orderId, (await checkDb.OrderedItems.AsNoTracking().SingleAsync(i => i.ItemId == itemId)).OrderId);
    }
}
