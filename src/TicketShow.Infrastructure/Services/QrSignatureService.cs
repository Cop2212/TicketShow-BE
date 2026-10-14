using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using QRCoder;
using TicketShow.Application.Common.Interfaces;

namespace TicketShow.Infrastructure.Services;

public class QrSignatureService : IQrSignatureService
{
    private readonly byte[] _secretKeyBytes;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public QrSignatureService(IConfiguration configuration)
    {
        var secret = configuration["QrCode:SecretKey"]
            ?? "IndustrialSuperSecretKeyForTicketShowHMAC2026SecureHash999!";
        _secretKeyBytes = Encoding.UTF8.GetBytes(secret);
    }

    public string GenerateQRSignedData(string ticketCode, Guid eventId, string customerEmail)
    {
        var payload = new QrPayload(
            TicketCode: ticketCode,
            EventId: eventId,
            Email: customerEmail,
            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );

        var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);

        using var hmac = new HMACSHA256(_secretKeyBytes);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson));
        var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var container = new
        {
            payload,
            signature
        };

        return JsonSerializer.Serialize(container, _jsonOptions);
    }

    public bool VerifyQRSignedData(string qrCodeString)
    {
        try
        {
            using var doc = JsonDocument.Parse(qrCodeString);
            var root = doc.RootElement;

            if (!root.TryGetProperty("payload", out var payloadElement) ||
                !root.TryGetProperty("signature", out var signatureElement))
            {
                return false;
            }

            var signature = signatureElement.GetString();
            if (string.IsNullOrWhiteSpace(signature)) return false;

            var payloadJson = payloadElement.GetRawText();

            using var hmac = new HMACSHA256(_secretKeyBytes);
            var expectedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson));
            var expectedSignature = Convert.ToHexString(expectedBytes).ToLowerInvariant();

            var sigBytes = Encoding.UTF8.GetBytes(signature.ToLowerInvariant());
            var expBytes = Encoding.UTF8.GetBytes(expectedSignature);

            // Constant-time comparison to protect against timing attacks
            return CryptographicOperations.FixedTimeEquals(sigBytes, expBytes);
        }
        catch
        {
            return false;
        }
    }

    public QrPayload? ParseQrPayload(string qrCodeString)
    {
        try
        {
            using var doc = JsonDocument.Parse(qrCodeString);
            var root = doc.RootElement;
            if (root.TryGetProperty("payload", out var payloadElement))
            {
                return JsonSerializer.Deserialize<QrPayload>(payloadElement.GetRawText(), _jsonOptions);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public string GenerateQrBase64Image(string qrData)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
        using var pngQrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImageBytes = pngQrCode.GetGraphic(20);
        return Convert.ToBase64String(qrCodeImageBytes);
    }
}
