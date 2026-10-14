using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TicketShow.Application.Common.Interfaces;

namespace TicketShow.Infrastructure.Services;

public class RedisLockService : ILockService
{
    private readonly ILogger<RedisLockService> _logger;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ConcurrentDictionary<string, (string Owner, DateTime ExpiresAt)> _memoryLocks = new();
    private readonly bool _useRedis;

    public RedisLockService(IConfiguration configuration, ILogger<RedisLockService> logger)
    {
        _logger = logger;
        var redisConn = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        try
        {
            var options = ConfigurationOptions.Parse(redisConn);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 1500;
            _redis = ConnectionMultiplexer.Connect(options);
            _useRedis = _redis.IsConnected;
            if (_useRedis)
            {
                _logger.LogInformation("Connected to Redis successfully for distributed locks.");
            }
            else
            {
                _logger.LogWarning("Redis is not reachable. Falling back to In-Memory Lock mode.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to connect to Redis ({Message}). Using In-Memory Lock mode.", ex.Message);
            _useRedis = false;
        }
    }

    public async Task<bool> LockSeatAsync(Guid eventId, Guid seatId, string lockOwnerId, TimeSpan? ttl = null)
    {
        var key = $"lock:event:{eventId}:seat:{seatId}";
        var expiry = ttl ?? TimeSpan.FromMinutes(5); // 300 seconds TTL per spec

        if (_useRedis && _redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                // SETNX with TTL (When.NotExists)
                return await db.StringSetAsync(key, lockOwnerId, expiry, When.NotExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis error during LockSeatAsync. Falling back to memory.");
            }
        }

        // In-memory fallback
        CleanExpiredMemoryLocks();
        var expiresAt = DateTime.UtcNow.Add(expiry);
        return _memoryLocks.TryAdd(key, (lockOwnerId, expiresAt));
    }

    public async Task<bool> ReleaseSeatLockAsync(Guid eventId, Guid seatId, string lockOwnerId)
    {
        var key = $"lock:event:{eventId}:seat:{seatId}";

        if (_useRedis && _redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                var currentVal = await db.StringGetAsync(key);
                if (currentVal == lockOwnerId)
                {
                    return await db.KeyDeleteAsync(key);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis error during ReleaseSeatLockAsync.");
            }
        }

        if (_memoryLocks.TryGetValue(key, out var lockInfo) && lockInfo.Owner == lockOwnerId)
        {
            return _memoryLocks.TryRemove(key, out _);
        }

        return false;
    }

    public async Task<bool> IsSeatLockedAsync(Guid eventId, Guid seatId)
    {
        var key = $"lock:event:{eventId}:seat:{seatId}";

        if (_useRedis && _redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                return await db.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis error during IsSeatLockedAsync.");
            }
        }

        CleanExpiredMemoryLocks();
        return _memoryLocks.ContainsKey(key);
    }

    private void CleanExpiredMemoryLocks()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _memoryLocks)
        {
            if (kvp.Value.ExpiresAt <= now)
            {
                _memoryLocks.TryRemove(kvp.Key, out _);
            }
        }
    }
}
