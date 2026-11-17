using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TicketShow.Infrastructure.Services;
using Xunit;

namespace TicketShow.UnitTests;

public class RedisLockConcurrencyTests
{
    private readonly RedisLockService _lockService;

    public RedisLockConcurrencyTests()
    {
        // Setup configuration with an invalid Redis host so it automatically tests the In-Memory Fallback engine
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:Redis", "127.0.0.1:9999" } // unreachable host -> triggers in-memory fallback
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var loggerMock = new Mock<ILogger<RedisLockService>>();

        _lockService = new RedisLockService(configuration, loggerMock.Object);
    }

    [Fact]
    public async Task LockSeatAsync_WhenSingleUserLocksAvailableSeat_ShouldReturnTrue()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var ownerId = "ORD-TEST-001";

        // Act
        var result = await _lockService.LockSeatAsync(eventId, seatId, ownerId, TimeSpan.FromMinutes(5));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task LockSeatAsync_WhenTwentyUsersCompeteForSameSeatSimultaneously_OnlyOneShouldSucceed()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        const int concurrentAttempts = 20;

        var tasks = new List<Task<bool>>();
        for (int i = 1; i <= concurrentAttempts; i++)
        {
            var userSession = $"ORD-USER-{i:D3}";
            tasks.Add(Task.Run(() => _lockService.LockSeatAsync(eventId, seatId, userSession, TimeSpan.FromMinutes(5))));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r == true);
        var failCount = results.Count(r => r == false);

        successCount.Should().Be(1, "chỉ duy nhất 1 khách hàng được phép giữ ghế khi có tranh chấp đồng thời");
        failCount.Should().Be(concurrentAttempts - 1, "tất cả các khách hàng khác phải nhận kết quả thất bại để tránh trùng lặp");
    }

    [Fact]
    public async Task ReleaseSeatLockAsync_WhenOwnerReleases_OtherUserCanAcquireLock()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var owner1 = "ORD-USER-001";
        var owner2 = "ORD-USER-002";

        // Act 1: User 1 locks seat
        var lockedBy1 = await _lockService.LockSeatAsync(eventId, seatId, owner1, TimeSpan.FromMinutes(5));
        lockedBy1.Should().BeTrue();

        // Act 2: User 2 tries while locked -> should fail
        var lockedBy2Attempt1 = await _lockService.LockSeatAsync(eventId, seatId, owner2, TimeSpan.FromMinutes(5));
        lockedBy2Attempt1.Should().BeFalse();

        // Act 3: User 1 releases lock
        var released = await _lockService.ReleaseSeatLockAsync(eventId, seatId, owner1);
        released.Should().BeTrue();

        // Act 4: User 2 tries again -> should succeed now
        var lockedBy2Attempt2 = await _lockService.LockSeatAsync(eventId, seatId, owner2, TimeSpan.FromMinutes(5));
        lockedBy2Attempt2.Should().BeTrue();
    }
}
