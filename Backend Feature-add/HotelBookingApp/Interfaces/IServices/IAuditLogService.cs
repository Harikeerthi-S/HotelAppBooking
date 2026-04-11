using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IAuditLogService
    {
        // ── CREATE ─────────────────────────────
        Task<AuditLogResponseDto> CreateAsync(CreateAuditLogDto dto);

        // ── GET BY ID ──────────────────────────
        Task<AuditLogResponseDto?> GetByIdAsync(int auditLogId);

        // ── GET ALL (PAGED) ────────────────────
        Task<PagedResponseDto<AuditLogResponseDto>> GetAllAsync(PagedRequestDto request);

        // ── FILTER ─────────────────────────────
        Task<List<AuditLogResponseDto>> FilterAsync(AuditLogFilterDto filter);

        // ── FILTER PAGED ───────────────────────
        Task<PagedResponseDto<AuditLogResponseDto>> FilterPagedAsync(
            AuditLogFilterDto filter,
            PagedRequestDto request
        );

        // ── GET BY ENTITY ──────────────────────
        Task<List<AuditLogResponseDto>> GetByEntityAsync(string entityName, int entityId);

        // ── GET BY USER ────────────────────────
        Task<List<AuditLogResponseDto>> GetByUserAsync(int userId);

        // ── DELETE ─────────────────────────────
        Task<bool> DeleteAsync(int auditLogId);
    }
}