using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TicketShow.Infrastructure.Services;
using Xunit;

namespace TicketShow.UnitTests;

public class QrSignatureSecurityTests
{
    private readonly QrSignatureService _qrService;

    public QrSignatureSecurityTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "QrCode:SecretKey", "TopSecretIndustrialTestHMACKeyForGraduationProject2026!" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _qrService = new QrSignatureService(configuration);
    }

    [Fact]
    public void GenerateQRSignedData_WithValidInput_ShouldProduceVerifiableQr()
    {
        // Arrange
        var ticketCode = "TICK-20261115-VIP999";
        var eventId = Guid.NewGuid();
        var email = "customer@example.com";

        // Act
        var qrString = _qrService.GenerateQRSignedData(ticketCode, eventId, email);

        // Assert
        qrString.Should().NotBeNullOrWhiteSpace();
        var isValid = _qrService.VerifyQRSignedData(qrString);
        isValid.Should().BeTrue("chữ ký HMAC hợp lệ phải được xác thực thành công");
    }

    [Fact]
    public void VerifyQRSignedData_WhenPayloadIsTampered_ShouldReturnFalse()
    {
        // Arrange
        var ticketCode = "TICK-VIP-ORIGINAL";
        var eventId = Guid.NewGuid();
        var email = "legit@example.com";

        var legitimateQr = _qrService.GenerateQRSignedData(ticketCode, eventId, email);

        // Act: Attacker modifies the ticketCode inside payload to steal a VIP seat
        var tamperedQr = legitimateQr.Replace("TICK-VIP-ORIGINAL", "TICK-VIP-HACKED");

        // Assert
        var isValid = _qrService.VerifyQRSignedData(tamperedQr);
        isValid.Should().BeFalse("bất kỳ sự can thiệp nào vào nội dung vé đều phải khiến chữ ký HMAC không khớp");
    }

    [Fact]
    public void VerifyQRSignedData_WhenSignatureIsForged_ShouldReturnFalse()
    {
        // Arrange
        var ticketCode = "TICK-VALID";
        var eventId = Guid.NewGuid();
        var email = "test@example.com";

        var legitimateQr = _qrService.GenerateQRSignedData(ticketCode, eventId, email);

        // Parse and change signature bytes
        var doc = JsonDocument.Parse(legitimateQr);
        var payloadRaw = doc.RootElement.GetProperty("payload").GetRawText();
        var forgedQr = $"{{\"payload\":{payloadRaw},\"signature\":\"0000000000000000000000000000000000000000000000000000000000000000\"}}";

        // Assert
        var isValid = _qrService.VerifyQRSignedData(forgedQr);
        isValid.Should().BeFalse("chữ ký giả mạo không thể qua được cổng kiểm tra HMAC");
    }

    [Fact]
    public void ParseQrPayload_WhenValidJson_ShouldExtractTicketDetailsCorrectly()
    {
        // Arrange
        var ticketCode = "TICK-PARSER-TEST";
        var eventId = Guid.NewGuid();
        var email = "checkin@example.com";

        var qrString = _qrService.GenerateQRSignedData(ticketCode, eventId, email);

        // Act
        var payload = _qrService.ParseQrPayload(qrString);

        // Assert
        payload.Should().NotBeNull();
        payload!.TicketCode.Should().Be(ticketCode);
        payload.EventId.Should().Be(eventId);
        payload.Email.Should().Be(email);
    }
}
