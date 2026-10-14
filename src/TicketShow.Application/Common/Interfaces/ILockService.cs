namespace TicketShow.Application.Common.Interfaces;

public interface ILockService
{
    Task<bool> LockSeatAsync(Guid eventId, Guid seatId, string lockOwnerId, TimeSpan? ttl = null);
    Task<bool> ReleaseSeatLockAsync(Guid eventId, Guid seatId, string lockOwnerId);
    Task<bool> IsSeatLockedAsync(Guid eventId, Guid seatId);
}

public record QrPayload(string TicketCode, Guid EventId, string Email, long Timestamp);

public interface IQrSignatureService
{
    string GenerateQRSignedData(string ticketCode, Guid eventId, string customerEmail);
    bool VerifyQRSignedData(string qrCodeString);
    QrPayload? ParseQrPayload(string qrCodeString);
    string GenerateQrBase64Image(string qrData);
}
