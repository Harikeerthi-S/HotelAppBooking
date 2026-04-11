using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IRepository<int, AuditLog> _auditRepo;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            IRepository<int, AuditLog> auditRepo,
            ILogger<AuditLogService> logger)
        {
            _auditRepo = auditRepo;
            _logger    = logger;
        }

        // ── CREATE ─────────────────────────────
        public async Task<AuditLogResponseDto> CreateAsync(CreateAuditLogDto dto)
        {
            _logger.LogInformation("Creating audit log: {Action}", dto.Action);

            var log = new AuditLog
            {
                UserId = dto.UserId,
                Action = dto.Action.Trim(),
                EntityName = dto.EntityName.Trim(),
                EntityId = dto.EntityId,
                Changes = dto.Changes,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _auditRepo.AddAsync(log);

            return MapToDto(created, null);
        }

        // ── GET BY ID ──────────────────────────
        public async Task<AuditLogResponseDto?> GetByIdAsync(int auditLogId)
        {
            var logs = await _auditRepo.GetAllIncludingAsync(a => a.User!);
            var log  = logs.FirstOrDefault(a => a.AuditLogId == auditLogId)
                ?? throw new NotFoundException("AuditLog", auditLogId);
            return MapToDto(log, log.User?.UserName);
        }

        // ── GET ALL (PAGED) ────────────────────
        public async Task<PagedResponseDto<AuditLogResponseDto>> GetAllAsync(PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 10);

            // Eager-load User in one query — no N+1
            var all     = await _auditRepo.GetAllIncludingAsync(a => a.User!);
            var ordered = all.OrderByDescending(a => a.CreatedAt).ToList();
            var total   = ordered.Count;

            var data = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => MapToDto(a, a.User?.UserName))
                .ToList();

            return new PagedResponseDto<AuditLogResponseDto>
            {
                Data         = data,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
            };
        }

        // ── FILTER (NEW) ───────────────────────
        public async Task<List<AuditLogResponseDto>> FilterAsync(AuditLogFilterDto filter)
        {
            var logs = await _auditRepo.GetAllAsync();

            var query = ApplyFilter(logs.AsQueryable(), filter)
                .OrderByDescending(a => a.CreatedAt);

            return query
                .Select(a => MapToDto(a, null))
                .ToList();
        }

        // ── FILTER PAGED (NEW) ─────────────────
        public async Task<PagedResponseDto<AuditLogResponseDto>> FilterPagedAsync(
            AuditLogFilterDto filter,
            PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 500);

            // Eager-load User in one query
            var all   = await _auditRepo.GetAllIncludingAsync(a => a.User!);
            var query = ApplyFilter(all.AsQueryable(), filter)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            var total = query.Count;

            var data = query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => MapToDto(a, a.User?.UserName))
                .ToList();

            return new PagedResponseDto<AuditLogResponseDto>
            {
                Data         = data,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
            };
        }

        // ── GET BY ENTITY ──────────────────────
        public async Task<List<AuditLogResponseDto>> GetByEntityAsync(string entityName, int entityId)
        {
            var logs = await _auditRepo.FindAllAsync(
                a => a.EntityName.ToLower() == entityName.ToLower().Trim()
                  && a.EntityId == entityId
            );

            return logs
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => MapToDto(a, null))
                .ToList();
        }

        // ── GET BY USER ────────────────────────
        public async Task<List<AuditLogResponseDto>> GetByUserAsync(int userId)
        {
            var logs = await _auditRepo.FindAllAsync(a => a.UserId == userId);

            return logs
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => MapToDto(a, null))
                .ToList();
        }

        // ── DELETE ─────────────────────────────
        public async Task<bool> DeleteAsync(int auditLogId)
        {
            _logger.LogInformation("Deleting audit log {AuditLogId}", auditLogId);

            var log = await _auditRepo.GetByIdAsync(auditLogId)
                ?? throw new NotFoundException("AuditLog", auditLogId);

            await _auditRepo.DeleteAsync(auditLogId);

            return true;
        }

        // ── FILTER LOGIC ───────────────────────
        private static IQueryable<AuditLog> ApplyFilter(IQueryable<AuditLog> query, AuditLogFilterDto filter)
        {
            if (filter.UserId.HasValue)
                query = query.Where(a => a.UserId == filter.UserId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Action))
                query = query.Where(a =>
                    a.Action.ToLower().Contains(filter.Action.ToLower().Trim()));

            if (!string.IsNullOrWhiteSpace(filter.EntityName))
                query = query.Where(a =>
                    a.EntityName.ToLower() == filter.EntityName.ToLower().Trim());

            if (filter.EntityId.HasValue)
                query = query.Where(a => a.EntityId == filter.EntityId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= filter.FromDate.Value.Date);

            if (filter.ToDate.HasValue)
                query = query.Where(a => a.CreatedAt < filter.ToDate.Value.Date.AddDays(1));

            return query;
        }

        // ── MAPPER ────────────────────────────
        private static AuditLogResponseDto MapToDto(AuditLog a, string? userName) => new()
        {
            AuditLogId = a.AuditLogId,
            UserId = a.UserId,
            UserName = userName ?? string.Empty,
            Action = a.Action,
            EntityName = a.EntityName,
            EntityId = a.EntityId,
            Changes = a.Changes ?? string.Empty,
            CreatedAt = a.CreatedAt
        };
    }
}