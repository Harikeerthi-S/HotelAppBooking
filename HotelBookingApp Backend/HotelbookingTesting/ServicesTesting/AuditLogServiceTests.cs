using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class AuditLogServiceTests
{
    private readonly Mock<IRepository<int, AuditLog>> _auditRepo = new();
    private readonly Mock<ILogger<AuditLogService>>   _logger    = new();

    private AuditLogService CreateService() =>
        new(_auditRepo.Object, _logger.Object);

    // ── CreateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsAuditLogResponseDto()
    {
        var dto = new CreateAuditLogDto
        {
            Action     = "HotelCreated",
            EntityName = "Hotel",
            EntityId   = 1,
            Changes    = "Name:Grand Palace"
        };

        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                  .ReturnsAsync((AuditLog a) => { a.AuditLogId = 1; return a; });

        var result = await CreateService().CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("HotelCreated", result.Action);
        Assert.Equal("Hotel", result.EntityName);
        Assert.Equal(1, result.EntityId);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtToUtcNow()
    {
        var dto = new CreateAuditLogDto { Action = "Test", EntityName = "Room", EntityId = 5 };
        var before = DateTime.UtcNow;

        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                  .ReturnsAsync((AuditLog a) => { a.AuditLogId = 2; return a; });

        var result = await CreateService().CreateAsync(dto);

        Assert.True(result.CreatedAt >= before);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingLog_ReturnsDto()
    {
        var log = new AuditLog
        {
            AuditLogId = 1, Action = "BookingCreated",
            EntityName = "Booking", EntityId = 10, CreatedAt = DateTime.UtcNow
        };

        _auditRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                  .ReturnsAsync(new List<AuditLog> { log });

        var result = await CreateService().GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("BookingCreated", result!.Action);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _auditRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                  .ReturnsAsync(new List<AuditLog>());

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByIdAsync(99));
    }

    // ── GetByEntityAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByEntityAsync_ReturnsMatchingLogs()
    {
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = 1, Action = "HotelCreated",  EntityName = "Hotel", EntityId = 5, CreatedAt = DateTime.UtcNow },
            new() { AuditLogId = 2, Action = "HotelUpdated",  EntityName = "Hotel", EntityId = 5, CreatedAt = DateTime.UtcNow },
            new() { AuditLogId = 3, Action = "BookingCreated",EntityName = "Booking",EntityId = 1, CreatedAt = DateTime.UtcNow }
        };

        _auditRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, bool>>>()))
                  .ReturnsAsync(logs.Where(l => l.EntityName == "Hotel" && l.EntityId == 5).ToList());

        var result = await CreateService().GetByEntityAsync("Hotel", 5);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal("Hotel", r.EntityName));
    }

    // ── GetByUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserAsync_ReturnsUserLogs()
    {
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = 1, UserId = 1, Action = "BookingCreated", EntityName = "Booking", CreatedAt = DateTime.UtcNow }
        };

        _auditRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, bool>>>()))
                  .ReturnsAsync(logs);

        var result = await CreateService().GetByUserAsync(1);

        Assert.Single(result);
        Assert.Equal(1, result[0].UserId);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingLog_ReturnsTrue()
    {
        var log = new AuditLog { AuditLogId = 1, Action = "Test", EntityName = "Hotel" };
        _auditRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(log);
        _auditRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(log);

        var result = await CreateService().DeleteAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        _auditRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((AuditLog?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().DeleteAsync(99));
    }

    // ── GetAllAsync (paged) ───────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        var logs = Enumerable.Range(1, 15).Select(i => new AuditLog
        {
            AuditLogId = i, Action = $"Action{i}", EntityName = "Hotel", CreatedAt = DateTime.UtcNow
        }).ToList();

        _auditRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                  .ReturnsAsync(logs);

        var result = await CreateService().GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Equal(10, result.Data.Count);
        Assert.Equal(15, result.TotalRecords);
        Assert.Equal(2, result.TotalPages);
    }
}
