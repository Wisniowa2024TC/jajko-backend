using System.Net;
using System.Text;
using System.Text.Json;
using JajkoBackend.Services.Blich;
using Microsoft.Extensions.Options;

namespace JajkoBackend.Tests;

public class BankBlikClientTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return respond(request);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string json) =>
        new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static (BankBlikClient Client, StubHandler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://bank.test/") };
        var options = Options.Create(new BankOptions { BaseUrl = "http://bank.test", MerchantId = "STORE_1" });
        return (new BankBlikClient(http, options), handler);
    }

    [Fact]
    public async Task Initiate_SendsDocumentedBody_AndParsesPaymentId()
    {
        var id = Guid.NewGuid();
        var (client, handler) = Create(_ => Json(HttpStatusCode.OK, $$"""{"paymentId":"{{id}}","message":"Waiting"}"""));

        var result = await client.InitiateAsync("481920", 45.50m);

        Assert.True(result.Success);
        Assert.Equal(id, result.PaymentId);
        Assert.Equal("http://bank.test/api/Blih/payment/initiate", handler.Request!.RequestUri!.ToString());
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("481920", body.RootElement.GetProperty("blihCode").GetString());
        Assert.Equal("STORE_1", body.RootElement.GetProperty("merchantId").GetString());
        Assert.Equal(45.50m, body.RootElement.GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task Initiate_On400_ReturnsBankMessage()
    {
        var (client, _) = Create(_ => Json(HttpStatusCode.BadRequest, """{"message":"Invalid or expired BLIH code."}"""));

        var result = await client.InitiateAsync("000000", 10m);

        Assert.False(result.Success);
        Assert.Equal("Invalid or expired BLIH code.", result.Error);
    }

    [Fact]
    public async Task Initiate_On500_ThrowsBankUnavailable()
    {
        var (client, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<BankUnavailableException>(() => client.InitiateAsync("123456", 10m));
    }

    [Fact]
    public async Task Initiate_OnNetworkError_ThrowsBankUnavailable()
    {
        var (client, _) = Create(_ => throw new HttpRequestException("boom"));

        await Assert.ThrowsAsync<BankUnavailableException>(() => client.InitiateAsync("123456", 10m));
    }

    [Theory]
    [InlineData("Pending", BankPaymentState.Pending)]
    [InlineData("Approved", BankPaymentState.Approved)]
    [InlineData("Failed", BankPaymentState.Failed)]
    public async Task Status_MapsBankValues(string status, BankPaymentState expected)
    {
        var id = Guid.NewGuid();
        var (client, handler) = Create(_ => Json(HttpStatusCode.OK, $$"""{"status":"{{status}}"}"""));

        Assert.Equal(expected, await client.GetStatusAsync(id));
        Assert.EndsWith($"/api/Blih/payment/{id}/status", handler.Request!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Status_On404_IsNotFound()
    {
        var (client, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        Assert.Equal(BankPaymentState.NotFound, await client.GetStatusAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Status_OnUnknownValue_ThrowsBankUnavailable()
    {
        var (client, _) = Create(_ => Json(HttpStatusCode.OK, """{"status":"Weird"}"""));

        await Assert.ThrowsAsync<BankUnavailableException>(() => client.GetStatusAsync(Guid.NewGuid()));
    }
}
