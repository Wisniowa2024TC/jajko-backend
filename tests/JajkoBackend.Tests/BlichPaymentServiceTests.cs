using JajkoBackend.Models;
using JajkoBackend.Services.Blich;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JajkoBackend.Tests;

public class BlichPaymentServiceTests : IDisposable
{
    private readonly TestEnv _env = new();
    public void Dispose() => _env.Dispose();

    private Task<StartResult> Start(int orderId, int userId = 1, string code = "123456") =>
        _env.InScope((svc, _) => svc.StartAsync(userId, orderId, code));

    private Task<BlichPayment?> Refresh(Guid id) => _env.InScope((svc, _) => svc.RefreshAsync(id));

    private Task<Order> LoadOrder(int id) =>
        _env.InScope((_, db) => db.Orders.AsNoTracking().SingleAsync(o => o.Id == id));

    [Fact]
    public async Task Start_CreatesPendingPayment_AndSendsOrderTotalToBank()
    {
        var orderId = await _env.SeedOrderAsync(total: 45.50m);

        var result = await Start(orderId, code: "481920");

        Assert.Equal(StartOutcome.Started, result.Outcome);
        Assert.Equal(BlichPaymentStatus.Pending, result.Payment!.Status);
        Assert.NotNull(result.Payment.BankPaymentId);
        Assert.Equal("481920", _env.Bank.LastCode);
        Assert.Equal(45.50m, _env.Bank.LastAmount);
        Assert.Equal(_env.Time.Now.UtcDateTime.AddSeconds(135), result.Payment.ExpiresAt);
    }

    [Fact]
    public async Task Start_ForSomeoneElsesOrder_IsNotFound_AndNeverCallsBank()
    {
        var orderId = await _env.SeedOrderAsync(userId: 1);

        var result = await Start(orderId, userId: 2);

        Assert.Equal(StartOutcome.OrderNotFound, result.Outcome);
        Assert.Equal(0, _env.Bank.InitiateCalls);
    }

    [Fact]
    public async Task Start_WhileAnotherPaymentIsActive_IsConflict()
    {
        var orderId = await _env.SeedOrderAsync();
        await Start(orderId);

        var second = await Start(orderId);

        Assert.Equal(StartOutcome.ActivePaymentExists, second.Outcome);
        Assert.Equal(1, _env.Bank.InitiateCalls);
    }

    [Fact]
    public async Task Start_WhenBankRejects_MarksFailed_AndAllowsRetry()
    {
        var orderId = await _env.SeedOrderAsync();
        _env.Bank.RejectWith = "Invalid or expired BLIH code.";

        var first = await Start(orderId);

        Assert.Equal(StartOutcome.BankRejected, first.Outcome);
        Assert.Equal("Invalid or expired BLIH code.", first.Message);

        _env.Bank.RejectWith = null;
        _env.Time.Advance(TimeSpan.FromSeconds(5));
        var retry = await Start(orderId);
        Assert.Equal(StartOutcome.Started, retry.Outcome);

        var list = await _env.InScope((svc, _) => svc.ListForOrderAsync(1, orderId));
        Assert.Equal([BlichPaymentStatus.Pending, BlichPaymentStatus.Failed], list!.Select(p => p.Status));
    }

    [Fact]
    public async Task Start_WhenBankUnavailable_Throws_MarksFailed_AndAllowsRetry()
    {
        var orderId = await _env.SeedOrderAsync();
        _env.Bank.InitiateUnavailable = true;

        await Assert.ThrowsAsync<BankUnavailableException>(() => Start(orderId));

        _env.Bank.InitiateUnavailable = false;
        Assert.Equal(StartOutcome.Started, (await Start(orderId)).Outcome);
    }

    [Fact]
    public async Task Start_ForPaidOrder_IsRejected()
    {
        var orderId = await _env.SeedOrderAsync();
        await _env.InScope((_, db) => db.Orders.Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.PaymentStatus, OrderPaymentStatus.Paid)));

        Assert.Equal(StartOutcome.OrderAlreadyPaid, (await Start(orderId)).Outcome);
    }

    [Fact]
    public async Task Start_ForZeroTotalOrder_IsRejected()
    {
        var orderId = await _env.SeedOrderAsync(total: 0m);

        Assert.Equal(StartOutcome.InvalidOrderTotal, (await Start(orderId)).Outcome);
    }

    [Fact]
    public async Task Refresh_WhenBankApproves_ApprovesPayment_AndMarksOrderPaid()
    {
        var orderId = await _env.SeedOrderAsync();
        var payment = (await Start(orderId)).Payment!;
        _env.Bank.State = BankPaymentState.Approved;

        var refreshed = await Refresh(payment.Id);

        Assert.Equal(BlichPaymentStatus.Approved, refreshed!.Status);
        Assert.NotNull(refreshed.CompletedAt);
        var order = await LoadOrder(orderId);
        Assert.Equal(OrderPaymentStatus.Paid, order.PaymentStatus);
        Assert.NotNull(order.PaidAt);
    }

    [Fact]
    public async Task Refresh_IsIdempotent_AfterApproval()
    {
        var orderId = await _env.SeedOrderAsync();
        var payment = (await Start(orderId)).Payment!;
        _env.Bank.State = BankPaymentState.Approved;
        await Refresh(payment.Id);
        var paidAt = (await LoadOrder(orderId)).PaidAt;
        var calls = _env.Bank.StatusCalls;

        _env.Time.Advance(TimeSpan.FromSeconds(10));
        await Refresh(payment.Id);

        Assert.Equal(calls, _env.Bank.StatusCalls);
        Assert.Equal(paidAt, (await LoadOrder(orderId)).PaidAt);
    }

    [Fact]
    public async Task Refresh_WhenBankFails_MarksFailed_AndLeavesOrderUnpaid()
    {
        var orderId = await _env.SeedOrderAsync();
        var payment = (await Start(orderId)).Payment!;
        _env.Bank.State = BankPaymentState.Failed;

        var refreshed = await Refresh(payment.Id);

        Assert.Equal(BlichPaymentStatus.Failed, refreshed!.Status);
        Assert.Equal(OrderPaymentStatus.Unpaid, (await LoadOrder(orderId)).PaymentStatus);
    }

    [Fact]
    public async Task Refresh_WhenStillPendingAfterDeadline_Expires()
    {
        var orderId = await _env.SeedOrderAsync();
        var payment = (await Start(orderId)).Payment!;
        _env.Time.Advance(TimeSpan.FromSeconds(136));

        var refreshed = await Refresh(payment.Id);

        Assert.Equal(BlichPaymentStatus.Expired, refreshed!.Status);
        Assert.Equal(OrderPaymentStatus.Unpaid, (await LoadOrder(orderId)).PaymentStatus);
    }

    [Fact]
    public async Task Refresh_HonoursApproval_EvenAfterDeadline()
    {
        var orderId = await _env.SeedOrderAsync();
        var payment = (await Start(orderId)).Payment!;
        _env.Time.Advance(TimeSpan.FromSeconds(140));
        _env.Bank.State = BankPaymentState.Approved;

        var refreshed = await Refresh(payment.Id);

        Assert.Equal(BlichPaymentStatus.Approved, refreshed!.Status);
        Assert.Equal(OrderPaymentStatus.Paid, (await LoadOrder(orderId)).PaymentStatus);
    }

    [Fact]
    public async Task Refresh_IsThrottled_ToOneBankCallPerSecond()
    {
        var orderId = await _env.SeedOrderAsync();
        var payment = (await Start(orderId)).Payment!;

        await Refresh(payment.Id);
        await Refresh(payment.Id);
        Assert.Equal(1, _env.Bank.StatusCalls);

        _env.Time.Advance(TimeSpan.FromSeconds(1.5));
        await Refresh(payment.Id);
        Assert.Equal(2, _env.Bank.StatusCalls);
    }

    [Fact]
    public async Task Refresh_WhenBankUnavailable_KeepsPaymentPending()
    {
        var orderId = await _env.SeedOrderAsync();
        var payment = (await Start(orderId)).Payment!;
        _env.Bank.StatusUnavailable = true;

        var refreshed = await Refresh(payment.Id);

        Assert.Equal(BlichPaymentStatus.Pending, refreshed!.Status);
    }

    [Fact]
    public async Task Get_ForAnotherUsersPayment_IsNull()
    {
        var orderId = await _env.SeedOrderAsync(userId: 1);
        var payment = (await Start(orderId)).Payment!;

        Assert.Null(await _env.InScope((svc, _) => svc.GetAsync(2, payment.Id)));
        Assert.NotNull(await _env.InScope((svc, _) => svc.GetAsync(1, payment.Id)));
    }

    [Fact]
    public async Task ListForOrder_ForAnotherUsersOrder_IsNull()
    {
        var orderId = await _env.SeedOrderAsync(userId: 1);

        Assert.Null(await _env.InScope((svc, _) => svc.ListForOrderAsync(2, orderId)));
    }

    [Fact]
    public async Task Reconciler_ApprovesPendingPayments_WithoutAnyUserRequest()
    {
        var orderId = await _env.SeedOrderAsync();
        await Start(orderId);
        _env.Bank.State = BankPaymentState.Approved;
        _env.Time.Advance(TimeSpan.FromSeconds(3));

        await RunReconciler();

        Assert.Equal(OrderPaymentStatus.Paid, (await LoadOrder(orderId)).PaymentStatus);
    }

    [Fact]
    public async Task Reconciler_FailsPaymentsStuckInInitiating_SoTheOrderCanBeRetried()
    {
        var orderId = await _env.SeedOrderAsync();
        await _env.InScope(async (_, db) =>
        {
            db.BlichPayments.Add(new BlichPayment
            {
                Id = Guid.NewGuid(), OrderId = orderId, UserId = 1, Amount = 45.50m, MerchantId = "TEST_MERCHANT",
                Status = BlichPaymentStatus.Initiating,
                CreatedAt = _env.Time.Now.UtcDateTime, ExpiresAt = _env.Time.Now.UtcDateTime.AddSeconds(135)
            });
            await db.SaveChangesAsync();
            return 0;
        });
        _env.Time.Advance(TimeSpan.FromSeconds(31));

        await RunReconciler();

        Assert.Equal(StartOutcome.Started, (await Start(orderId)).Outcome);
    }

    private async Task RunReconciler()
    {
        var reconciler = new BlichReconciliationService(
            _env.Services.GetRequiredService<IServiceScopeFactory>(), _env.Time,
            _env.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BlichReconciliationService>>());
        await reconciler.RunOnceAsync(CancellationToken.None);
    }
}
