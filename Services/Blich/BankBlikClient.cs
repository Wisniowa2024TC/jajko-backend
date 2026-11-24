using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace JajkoBackend.Services.Blich;

public class BankBlikClient(HttpClient http, IOptions<BankOptions> options) : IBankBlikClient
{
    private readonly BankOptions _options = options.Value;

    private sealed record InitiateBody(string BlihCode, string MerchantId, decimal Amount);
    private sealed record InitiateReply(Guid PaymentId, string? Message);
    private sealed record StatusReply(string? Status);
    private sealed record ErrorReply(string? Message);

    public async Task<BankInitiateResult> InitiateAsync(string blikCode, decimal amount, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(
                "api/Blih/payment/initiate",
                new InitiateBody(blikCode, _options.MerchantId, amount),
                JsonSerializerOptions.Web, ct);

            if (response.IsSuccessStatusCode)
            {
                var reply = await response.Content.ReadFromJsonAsync<InitiateReply>(JsonSerializerOptions.Web, ct);
                return reply is null || reply.PaymentId == Guid.Empty
                    ? throw new BankUnavailableException("Bank returned an empty initiate response.")
                    : new BankInitiateResult(true, reply.PaymentId, null);
            }

            if ((int)response.StatusCode >= 500)
            {
                throw new BankUnavailableException($"Bank responded with {(int)response.StatusCode}.");
            }

            return new BankInitiateResult(false, null, await ReadErrorAsync(response, ct));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       && !ct.IsCancellationRequested)
        {
            throw new BankUnavailableException("Could not reach the bank.", ex);
        }
    }

    public async Task<BankPaymentState> GetStatusAsync(Guid bankPaymentId, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync($"api/Blih/payment/{bankPaymentId}/status", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return BankPaymentState.NotFound;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new BankUnavailableException($"Bank responded with {(int)response.StatusCode}.");
            }

            var reply = await response.Content.ReadFromJsonAsync<StatusReply>(JsonSerializerOptions.Web, ct);
            return reply?.Status switch
            {
                "Pending" => BankPaymentState.Pending,
                "Approved" => BankPaymentState.Approved,
                "Failed" => BankPaymentState.Failed,
                _ => throw new BankUnavailableException($"Unknown bank payment status '{reply?.Status}'.")
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       && !ct.IsCancellationRequested)
        {
            throw new BankUnavailableException("Could not reach the bank.", ex);
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorReply>(JsonSerializerOptions.Web, ct);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch (JsonException)
        {
            // fall through to the generic message
        }

        return "The bank rejected the payment request.";
    }
}
