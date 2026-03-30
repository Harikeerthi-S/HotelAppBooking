using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    /// <summary>Creates and manages push notifications for users.</summary>
    public class NotificationService : INotificationService
    {
        private readonly IRepository<int, Notification> _notificationRepo;
        private readonly IRepository<int, User>         _userRepo;
        private readonly ILogger<NotificationService>   _logger;

        public NotificationService(
            IRepository<int, Notification> notificationRepo,
            IRepository<int, User>         userRepo,
            ILogger<NotificationService>   logger)
        {
            _notificationRepo = notificationRepo ?? throw new ArgumentNullException(nameof(notificationRepo));
            _userRepo         = userRepo         ?? throw new ArgumentNullException(nameof(userRepo));
            _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));
        }

        // ── CREATE ────────────────────────────────────────────────────────
        public async Task<NotificationResponseDto> CreateAsync(CreateNotificationDto dto)
        {
            _logger.LogInformation("Creating notification for User:{UserId}", dto.UserId);

            var user = await _userRepo.GetByIdAsync(dto.UserId)
                       ?? throw new NotFoundException("User", dto.UserId);

            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new BadRequestException("Notification message cannot be empty.");

            var notification = new Notification
            {
                UserId    = dto.UserId,
                Message   = dto.Message.Trim(),
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _notificationRepo.AddAsync(notification);
            _logger.LogInformation("Notification created: {NotificationId}", created.NotificationId);
            return MapToDto(created);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────
        public async Task<NotificationResponseDto?> GetByIdAsync(int notificationId)
        {
            var n = await _notificationRepo.GetByIdAsync(notificationId)
                    ?? throw new NotFoundException("Notification", notificationId);
            return MapToDto(n);
        }

        // ── GET BY USER ───────────────────────────────────────────────────
        public async Task<IEnumerable<NotificationResponseDto>> GetByUserAsync(int userId)
        {
            var list = await _notificationRepo.FindAllAsync(n => n.UserId == userId);
            return list.OrderByDescending(n => n.CreatedAt).Select(MapToDto).ToList();
        }

        // ── GET ALL (Admin) ───────────────────────────────────────────────
        public async Task<IEnumerable<NotificationResponseDto>> GetAllAsync()
        {
            var all = await _notificationRepo.GetAllAsync();
            return all.OrderByDescending(n => n.CreatedAt).Select(MapToDto).ToList();
        }

        // ── PAGED (all) ───────────────────────────────────────────────────
        public async Task<PagedResponseDto<NotificationResponseDto>> GetPagedAsync(PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 10);

            var all = await _notificationRepo.GetAllAsync();
            var ordered = all.OrderByDescending(n => n.CreatedAt).ToList();
            var total = ordered.Count;
            var data = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResponseDto<NotificationResponseDto>
            {
                Data         = data,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
            };
        }

        // ── PAGED BY USER ─────────────────────────────────────────────────
        public async Task<PagedResponseDto<NotificationResponseDto>> GetPagedByUserAsync(int userId, PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 100);

            var list = await _notificationRepo.FindAllAsync(n => n.UserId == userId);
            var ordered = list.OrderByDescending(n => n.CreatedAt).ToList();
            var total = ordered.Count;
            var data = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResponseDto<NotificationResponseDto>
            {
                Data         = data,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
            };
        }

        public async Task<int> GetUnreadCountForUserAsync(int userId)
        {
            var list = await _notificationRepo.FindAllAsync(n => n.UserId == userId && !n.IsRead);
            return list.Count();
        }

        public async Task<int> GetUnreadCountAllAsync()
        {
            var list = await _notificationRepo.FindAllAsync(n => !n.IsRead);
            return list.Count();
        }

        // ── MARK AS READ ──────────────────────────────────────────────────
        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            _logger.LogInformation("Marking notification {NotificationId} as read", notificationId);

            var notification = await _notificationRepo.GetByIdAsync(notificationId)
                               ?? throw new NotFoundException("Notification", notificationId);

            if (notification.IsRead) return true; // already read — idempotent

            notification.IsRead = true;
            await _notificationRepo.UpdateAsync(notificationId, notification);
            return true;
        }

        // ── DELETE ────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int notificationId)
        {
            _logger.LogInformation("Deleting notification {NotificationId}", notificationId);
            var deleted = await _notificationRepo.DeleteAsync(notificationId);
            if (deleted is null) throw new NotFoundException("Notification", notificationId);
            return true;
        }

        // ── MAPPER ────────────────────────────────────────────────────────
        private static NotificationResponseDto MapToDto(Notification n) => new()
        {
            NotificationId = n.NotificationId,
            UserId         = n.UserId,
            Message        = n.Message,
            IsRead         = n.IsRead,
            CreatedAt      = n.CreatedAt
        };
    }
}
