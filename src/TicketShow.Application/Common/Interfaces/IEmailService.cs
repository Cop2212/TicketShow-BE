namespace TicketShow.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendETicketEmailAsync(
        string toEmail,
        string customerName,
        string orderCode,
        string eventTitle,
        string venueName,
        DateTime startTime,
        List<(string TicketCode, string TierName, string SeatLabel, string QrBase64)> tickets,
        CancellationToken cancellationToken = default);
}
