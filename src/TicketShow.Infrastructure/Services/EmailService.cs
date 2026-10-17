using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TicketShow.Application.Common.Interfaces;

namespace TicketShow.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendETicketEmailAsync(
        string toEmail,
        string customerName,
        string orderCode,
        string eventTitle,
        string venueName,
        DateTime startTime,
        List<(string TicketCode, string TierName, string SeatLabel, string QrBase64)> tickets,
        CancellationToken cancellationToken = default)
    {
        var htmlContent = BuildETicketHtml(customerName, orderCode, eventTitle, venueName, startTime, tickets);

        var smtpHost = _configuration["Email:SmtpHost"];
        var smtpPortStr = _configuration["Email:SmtpPort"];
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var fromEmail = _configuration["Email:From"] ?? "no-reply@ticketshow.vn";

        if (!string.IsNullOrEmpty(smtpHost) && int.TryParse(smtpPortStr, out var smtpPort) && !string.IsNullOrEmpty(username))
        {
            try
            {
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "TicketShow Vietnam"),
                    Subject = $"[TicketShow] Vé điện tử chính thức - Đơn hàng {orderCode}",
                    Body = htmlContent,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);
                await client.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Successfully sent E-Ticket email for order {OrderCode} to {Email}", orderCode, toEmail);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email via SMTP, falling back to simulated email logger for order {OrderCode}", orderCode);
            }
        }

        // Fallback / Development Simulation
        _logger.LogInformation(
            "=== [SIMULATED E-TICKET EMAIL DISPATCH] ===\nTo: {ToEmail}\nCustomer: {CustomerName}\nOrder: {OrderCode}\nEvent: {EventTitle}\nTickets Count: {Count}\n==============================================",
            toEmail, customerName, orderCode, eventTitle, tickets.Count
        );
    }

    private static string BuildETicketHtml(
        string customerName,
        string orderCode,
        string eventTitle,
        string venueName,
        DateTime startTime,
        List<(string TicketCode, string TierName, string SeatLabel, string QrBase64)> tickets)
    {
        var sb = new StringBuilder();
        sb.Append($@"
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset='utf-8'>
          <style>
            body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f3f4f6; margin: 0; padding: 20px; color: #1f2937; }}
            .container {{ max-width: 650px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }}
            .header {{ background: linear-gradient(135deg, #4f46e5, #7c3aed); padding: 30px; text-align: center; color: white; }}
            .header h1 {{ margin: 0 0 8px 0; font-size: 24px; }}
            .header p {{ margin: 0; font-size: 15px; opacity: 0.9; }}
            .content {{ padding: 24px 30px; }}
            .event-box {{ background: #f9fafb; border-radius: 8px; border: 1px solid #e5e7eb; padding: 16px; margin-bottom: 24px; }}
            .event-title {{ font-size: 20px; font-weight: bold; color: #111827; margin: 0 0 8px 0; }}
            .event-meta {{ font-size: 14px; color: #4b5563; margin: 4px 0; }}
            .ticket-card {{ border: 2px dashed #d1d5db; border-radius: 10px; padding: 18px; margin-bottom: 20px; background: #faf5ff; display: flex; align-items: center; justify-content: space-between; }}
            .ticket-info {{ flex: 1; }}
            .ticket-code {{ font-family: monospace; font-size: 16px; font-weight: bold; color: #6d28d9; margin-bottom: 4px; }}
            .ticket-seat {{ font-size: 18px; font-weight: bold; color: #1f2937; margin: 6px 0; }}
            .ticket-tier {{ display: inline-block; padding: 2px 10px; border-radius: 9999px; background: #e9d5ff; color: #6b21a8; font-size: 12px; font-weight: 600; }}
            .qr-code {{ text-align: center; margin-left: 20px; }}
            .qr-code img {{ width: 130px; height: 130px; border-radius: 6px; border: 1px solid #e5e7eb; }}
            .footer {{ background: #f9fafb; padding: 20px; text-align: center; font-size: 13px; color: #6b7280; border-top: 1px solid #e5e7eb; }}
          </style>
        </head>
        <body>
          <div class='container'>
            <div class='header'>
              <h1>XÁC NHẬN VÉ ĐIỆN TỬ CHÍNH THỨC</h1>
              <p>Cảm ơn quý khách <strong>{WebUtility.HtmlEncode(customerName)}</strong>! Mã đơn: <strong>{orderCode}</strong></p>
            </div>
            <div class='content'>
              <div class='event-box'>
                <div class='event-title'>{WebUtility.HtmlEncode(eventTitle)}</div>
                <div class='event-meta'>📍 Địa điểm: <strong>{WebUtility.HtmlEncode(venueName)}</strong></div>
                <div class='event-meta'>⏰ Thời gian: <strong>{startTime:dd/MM/yyyy HH:mm} (Giờ Việt Nam)</strong></div>
              </div>
              <h3 style='margin: 0 0 16px 0; font-size: 16px; color: #374151;'>DANH SÁCH VÉ CỦA BẠN ({tickets.Count} vé):</h3>
        ");

        foreach (var t in tickets)
        {
            var qrImgSrc = t.QrBase64.StartsWith("data:image")
                ? t.QrBase64
                : $"data:image/png;base64,{t.QrBase64}";

            sb.Append($@"
              <div class='ticket-card'>
                <div class='ticket-info'>
                  <div class='ticket-code'>{WebUtility.HtmlEncode(t.TicketCode)}</div>
                  <div class='ticket-seat'>Ghế: {WebUtility.HtmlEncode(t.SeatLabel)}</div>
                  <span class='ticket-tier'>{WebUtility.HtmlEncode(t.TierName)}</span>
                  <p style='font-size: 12px; color: #6b7280; margin: 8px 0 0 0;'>Trình mã QR này tại cổng soát vé để vào cửa.</p>
                </div>
                <div class='qr-code'>
                  <img src='{qrImgSrc}' alt='QR Code' />
                  <div style='font-size: 11px; color: #9ca3af; margin-top: 4px;'>HMAC-SHA256 Protected</div>
                </div>
              </div>
            ");
        }

        sb.Append(@"
              <div style='margin-top: 24px; padding: 14px; background: #fef3c7; border-radius: 8px; font-size: 13px; color: #92400e;'>
                ⚠️ <strong>Lưu ý:</strong> Mỗi vé điện tử chỉ được quét qua cổng đúng 01 lần. Tuyệt đối không chia sẻ ảnh chụp mã QR cho người khác.
              </div>
            </div>
            <div class='footer'>
              Hệ thống bán vé thông minh TicketShow &copy; 2026. Mọi thắc mắc xin liên hệ support@ticketshow.vn
            </div>
          </div>
        </body>
        </html>
        ");

        return sb.ToString();
    }
}
