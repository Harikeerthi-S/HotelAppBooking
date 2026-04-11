using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/auditlog")]
    [Authorize(Roles = "admin")] // Only admin can access logs
    [Produces("application/json")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditService;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(
            IAuditLogService auditService,
            ILogger<AuditLogController> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        // ── CREATE ─────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAuditLogDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var log = await _auditService.CreateAsync(dto);

                return CreatedAtAction(nameof(GetById), new { auditLogId = log.AuditLogId }, log);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit log");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "Error creating audit log",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ── GET BY ID ──────────────────────────
        [HttpGet("{auditLogId:int}")]
        public async Task<IActionResult> GetById(int auditLogId)
        {
            try
            {
                var log = await _auditService.GetByIdAsync(auditLogId);
                return Ok(log);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ── GET ALL (PAGED) ────────────────────
        [HttpPost("all/paged")]
        public async Task<IActionResult> GetAll([FromBody] PagedRequestDto request)
        {
            try
            {
                var result = await _auditService.GetAllAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching audit logs");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "Error retrieving audit logs",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ── FILTER ─────────────────────────────
        [HttpPost("filter")]
        public async Task<IActionResult> Filter([FromBody] AuditLogFilterDto filter)
        {
            try
            {
                var result = await _auditService.FilterAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering audit logs");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "Error filtering audit logs",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ── FILTER PAGED ───────────────────────
        [HttpPost("filter/paged")]
        public async Task<IActionResult> FilterPaged([FromBody] AuditLogFilterPagedDto dto)
        {
            try
            {
                var request = new PagedRequestDto
                {
                    PageNumber = Math.Max(1, dto.PageNumber),
                    PageSize   = Math.Clamp(dto.PageSize, 1, 500)
                };
                var filter = new AuditLogFilterDto
                {
                    UserId     = dto.UserId,
                    Action     = string.IsNullOrWhiteSpace(dto.Action)     ? null : dto.Action,
                    EntityName = string.IsNullOrWhiteSpace(dto.EntityName) ? null : dto.EntityName,
                    EntityId   = dto.EntityId,
                    FromDate   = dto.FromDate,
                    ToDate     = dto.ToDate
                };
                var result = await _auditService.FilterPagedAsync(filter, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering audit logs (paged)");
                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "Error filtering audit logs",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ── GET BY ENTITY ──────────────────────
        [HttpGet("entity")]
        public async Task<IActionResult> GetByEntity(
            [FromQuery] string entityName,
            [FromQuery] int entityId)
        {
            try
            {
                var result = await _auditService.GetByEntityAsync(entityName, entityId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching logs by entity");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "Error retrieving audit logs",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ── GET BY USER ────────────────────────
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            try
            {
                var result = await _auditService.GetByUserAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching logs by user");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "Error retrieving audit logs",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ── DELETE ─────────────────────────────
        [HttpDelete("{auditLogId:int}")]
        public async Task<IActionResult> Delete(int auditLogId)
        {
            try
            {
                await _auditService.DeleteAsync(auditLogId);
                return Ok(new { message = "Audit log deleted successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audit log");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "Error deleting audit log",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}