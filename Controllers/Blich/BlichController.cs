using JajkoBackend.Helpers;
using JajkoBackend.Models.DTOs.Blich;
using JajkoBackend.Services.Blich;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JajkoBackend.Controllers.Blich;

[ApiController]
[Authorize]
[Route("api")]
public class BlichController(IBlichPaymentService payments) : ControllerBase
{
    public const string StartRateLimitPolicy = "blich-start";

    [HttpPost("orders/{orderId:int}/blich-payments")]
    [EnableRateLimiting(StartRateLimitPolicy)]
    [ProducesResponseType<BlichPaymentResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Start(int orderId, StartBlichPaymentRequest request, CancellationToken ct)
    {
        StartResult result;
        try
        {
            result = await payments.StartAsync(User.GetUserId(), orderId, request.BlikCode, ct);
        }
        catch (BankUnavailableException)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new MessageResponse("The bank is unavailable. Please try again in a moment."));
        }

        return result.Outcome switch
        {
            StartOutcome.Started => AcceptedAtAction(nameof(Get), new { paymentId = result.Payment!.Id },
                BlichPaymentResponse.From(result.Payment)),
            StartOutcome.OrderNotFound => NotFound(),
            StartOutcome.OrderAlreadyPaid or StartOutcome.ActivePaymentExists =>
                Conflict(new MessageResponse(result.Message!)),
            _ => BadRequest(new MessageResponse(result.Message ?? "Payment could not be started."))
        };
    }

    /// Payment status. The checkout page polls this every ~2 seconds.
    [HttpGet("blich-payments/{paymentId:guid}")]
    [ProducesResponseType<BlichPaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid paymentId, CancellationToken ct)
    {
        var payment = await payments.GetAsync(User.GetUserId(), paymentId, ct);
        return payment is null ? NotFound() : Ok(BlichPaymentResponse.From(payment));
    }

    /// All payment attempts for an order, newest first.
    [HttpGet("orders/{orderId:int}/blich-payments")]
    [ProducesResponseType<List<BlichPaymentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListForOrder(int orderId, CancellationToken ct)
    {
        var list = await payments.ListForOrderAsync(User.GetUserId(), orderId, ct);
        return list is null ? NotFound() : Ok(list.Select(BlichPaymentResponse.From));
    }
}
