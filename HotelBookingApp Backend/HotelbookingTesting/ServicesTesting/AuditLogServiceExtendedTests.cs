using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class AuditLogServiceExtendedTests
{
    private readonly Mock<IRepository<int, AuditLog>> _auditRepo = new();
    private readonly Mock<ILogger<AuditLogService>>   _logger    = new();

    private AuditLogService CreateService() =>
        new(_auditRepo.Object, _logger.Object);

    private static List<AuditLog> SampleLogs() => new()
    {
        new() { AuditLogId = 1, UserId = 1, Action = "BookingCreated",  EntityName = "Booking", EntityId = 10, CreatedAt = DateTime.UtcNow.AddDays(-2) },
        new() { AuditLogId = 2, UserId = 1, Action = "PaymentCreated",  EntityName = "Payment", EntityId = 10, CreatedAt = DateTime.UtcNow.AddDays(-1) },
        new() { AuditLogId = 3, UserId = 2, Action = "HotelCreated",    EntityName = "Hotel",   EntityId = 5,  CreatedAt = DateTime.UtcNow },
        new() { AuditLogId = 4, UserId = 2, Action = "BookingCancelled",EntityName = "Booking", EntityId = 11, CreatedAt = DateTime.UtcNow },
    };

    // ── FilterAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task FilterAsync_ByAction_ReturnsMatchingLogs()
    {
        _auditRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(SampleLogs());

        var result = await CreateService().FilterAsync(new AuditLogFilterDto { Action = "Booking" });

        Assert.Equal(2, result.Count); // BookingCreated + BookingCancelled
    }

    [Fact]
    public async Task FilterAsync_ByEntityName_ReturnsMatchingLogs()
    {
        _auditRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(SampleLogs());

        var result = await CreateService().FilterAsync(new AuditLogFilterDto { EntityName = "Hotel" });

        Assert.Single(result);
        Assert.Equal("HotelCreated", result[0].Action);
    }

    [Fact]
    public async Task FilterAsync_ByUserId_ReturnsUserLogs()
    {
        _auditRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(SampleLogs());

        var result = await CreateService().FilterAsync(new AuditLogFilterDto { UserId = 1 });

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(1, r.UserId));
    }

    [Fact]
    public async Task FilterAsync_ByEntityId_ReturnsMatchingLogs()
    {
        _auditRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(SampleLogs());

        var result = await CreateService().FilterAsync(new AuditLogFilterDto { EntityId = 10 });

        Assert.Equal(2, result.Count); // BookingCreated + PaymentCreated for EntityId=10
    }

    [Fact]
    public async Task FilterAsync_ByDateRange_ReturnsLogsInRange()
    {
        _auditRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(SampleLogs());

        var result = await CreateService().FilterAsync(new AuditLogFilterDto
        {
            FromDate = DateTime.UtcNow.AddDays(-1).Date,
            ToDate   = DateTime.UtcNow.AddDays(1)
        });

        Assert.Equal(3, result.Count); // logs from yesterday and today
    }

    // ── FilterPagedAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task FilterPagedAsync_ByAction_ReturnsPaginatedFilteredLogs()
    {
        var logs = Enumerable.Range(1, 20).Select(i => new AuditLog
        {
            AuditLogId = i, UserId = 1, Action = i % 2 == 0 ? "BookingCreated" : "HotelCreated",
            EntityName = i % 2 == 0 ? "Booking" : "Hotel", CreatedAt = DateTime.UtcNow
        }).ToList();

        _auditRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                  .ReturnsAsync(logs);

        var result = await CreateService().FilterPagedAsync(
            new AuditLogFilterDto { Action = "Booking" },
            new PagedRequestDto { PageNumber = 1, PageSize = 5 }
        );

        Assert.Equal(5, result.Data.Count);
        Assert.Equal(10, result.TotalRecords); // 10 BookingCreated logs
    }

    [Fact]
    public async Task FilterPagedAsync_NoFilter_ReturnsAllPaged()
    {
        var logs = Enumerable.Range(1, 25).Select(i => new AuditLog
        {
            AuditLogId = i, Action = "Test", EntityName = "Hotel", CreatedAt = DateTime.UtcNow
        }).ToList();

        _auditRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                  .ReturnsAsync(logs);

        var result = await CreateService().FilterPagedAsync(
            new AuditLogFilterDto(),
            new PagedRequestDto { PageNumber = 1, PageSize = 10 }
        );

        Assert.Equal(10, result.Data.Count);
        Assert.Equal(25, result.TotalRecords);
        Assert.Equal(3, result.TotalPages);
    }

    // ── GetByEntityAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByEntityAsync_ReturnsEntityLogs()
    {
        var logs = SampleLogs().Where(l => l.EntityName == "Booking" && l.EntityId == 10).ToList();
        _auditRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, bool>>>()))
                  .ReturnsAsync(logs);

        var result = await CreateService().GetByEntityAsync("Booking", 10);

        Assert.Single(result);
        Assert.Equal("BookingCreated", result[0].Action);
    }

    // ── GetAllAsync pagination ────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Page2_ReturnsSecondPage()
    {
        var logs = Enumerable.Range(1, 25).Select(i => new AuditLog
        {
            AuditLogId = i, Action = $"Action{i}", EntityName = "Hotel", CreatedAt = DateTime.UtcNow.AddSeconds(-i)
        }).ToList();

        _auditRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                  .ReturnsAsync(logs);

        var result = await CreateService().GetAllAsync(new PagedRequestDto { PageNumber = 2, PageSize = 10 });

        Assert.Equal(10, result.Data.Count);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.TotalRecords);
    }
}
