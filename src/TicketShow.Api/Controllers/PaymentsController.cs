using Microsoft.AspNetCore.Mvc;
using TicketShow.Application.Common.Models;
using TicketShow.Application.Features.Payments.Commands;

namespace TicketShow.Api.Controllers;

public class PaymentsController : BaseApiController
{
    /// <summary>
    /// Webhook callback từ cổng thanh toán (VNPay / Momo / QR Pay)
    /// </summary>
    [HttpPost("webhook")]
    public async Task<ActionResult<ApiResponse<PaymentProcessResultDto>>> HandleWebhook([FromBody] PaymentWebhookCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Mô phỏng thanh toán thành công tức thì (Dành cho kiểm thử & Demo Đồ án)
    /// </summary>
    [HttpPost("simulate")]
    public async Task<ActionResult<ApiResponse<PaymentProcessResultDto>>> SimulatePayment([FromBody] SimulatePaymentRequest request)
    {
        var command = new PaymentWebhookCommand
        {
            OrderId = request.OrderId,
            OrderCode = request.OrderCode,
            TransactionId = $"SIM-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            PaymentGateway = request.PaymentGateway ?? "QRPAY_SIMULATOR",
            Status = request.Success ? "SUCCESS" : "FAILED"
        };

        var result = await Mediator.Send(command);
        return Ok(result);
    }
}

public record SimulatePaymentRequest(
    Guid? OrderId,
    string? OrderCode,
    string? PaymentGateway,
    bool Success = true
);
