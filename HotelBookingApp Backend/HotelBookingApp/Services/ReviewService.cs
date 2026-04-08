using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Hosting;

namespace HotelBookingApp.Services
{
    /// <summary>Creates and manages hotel reviews — one review per user per hotel.</summary>
    public class ReviewService : IReviewService
    {
        private readonly IRepository<int, Review> _reviewRepo;
        private readonly IRepository<int, Hotel>  _hotelRepo;
        private readonly IRepository<int, User>   _userRepo;
        private readonly IWalletService           _walletService;
        private readonly IAuditLogService         _audit;
        private readonly ILogger<ReviewService>   _logger;

        public ReviewService(
            IRepository<int, Review> reviewRepo,
            IRepository<int, Hotel>  hotelRepo,
            IRepository<int, User>   userRepo,
            IWalletService           walletService,
            IAuditLogService         audit,
            ILogger<ReviewService>   logger)
        {
            _reviewRepo    = reviewRepo;
            _hotelRepo     = hotelRepo;
            _userRepo      = userRepo;
            _walletService = walletService;
            _audit         = audit;
            _logger        = logger;
        }

        private async Task LogAsync(string action, int? entityId, int? userId = null, string? changes = null)
        {
            try { await _audit.CreateAsync(new CreateAuditLogDto { UserId = userId, Action = action, EntityName = "Review", EntityId = entityId, Changes = changes }); }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed: {Action}", action); }
        }

        // ── CREATE ────────────────────────────────────────────────────────
        public async Task<ReviewResponseDto> CreateAsync(CreateReviewDto dto)
        {
            _logger.LogInformation("Creating review — Hotel:{HotelId} User:{UserId}", dto.HotelId, dto.UserId);

            if (dto.Rating < 1 || dto.Rating > 5)
                throw new BadRequestException("Rating must be between 1 and 5.");

            var hotel = await _hotelRepo.GetByIdAsync(dto.HotelId);
            if (hotel is null || !hotel.IsActive)
                throw new NotFoundException("Hotel", dto.HotelId);

            var user = await _userRepo.GetByIdAsync(dto.UserId)
                       ?? throw new NotFoundException("User", dto.UserId);

            // One review per user per hotel
            var alreadyReviewed = await _reviewRepo.ExistsAsync(
                r => r.HotelId == dto.HotelId && r.UserId == dto.UserId
            );
            if (alreadyReviewed)
                throw new AlreadyExistsException("You have already reviewed this hotel.");

            var review = new Review
            {
                HotelId   = dto.HotelId,
                UserId    = dto.UserId,
                Rating    = dto.Rating,
                Comment   = dto.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var created = await _reviewRepo.AddAsync(review);
            _logger.LogInformation("Review created: {ReviewId}", created.ReviewId);
            await LogAsync("ReviewCreated", created.ReviewId, dto.UserId,
                $"Hotel:{dto.HotelId} Rating:{dto.Rating}/5");
            return MapToDto(created);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────
        public async Task<ReviewResponseDto?> GetByIdAsync(int reviewId)
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId)
                         ?? throw new NotFoundException("Review", reviewId);
            return MapToDto(review);
        }

        // ── GET PAGED (with filter) ───────────────────────────────────────
        public async Task<PagedResponseDto<ReviewResponseDto>> GetPagedAsync(
            ReviewFilterDto filter, PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 10);

            var all   = await _reviewRepo.GetAllAsync();
            var query = all.AsQueryable();

            if (filter.HotelId.HasValue)
                query = query.Where(r => r.HotelId == filter.HotelId.Value);
            if (filter.UserId.HasValue)
                query = query.Where(r => r.UserId == filter.UserId.Value);
            if (filter.Rating.HasValue)
                query = query.Where(r => r.Rating == filter.Rating.Value);

            var ordered = query.OrderByDescending(r => r.CreatedAt);
            var total   = ordered.Count();
            var data    = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResponseDto<ReviewResponseDto>
            {
                Data         = data,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
            };
        }

        // ── UPLOAD PHOTO ──────────────────────────────────────────────────
        public async Task<ReviewResponseDto> UploadPhotoAsync(int reviewId, IFormFile photo, IWebHostEnvironment env)
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId)
                         ?? throw new NotFoundException("Review", reviewId);

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new BadRequestException("Only JPG, PNG, or WEBP images are allowed.");

            if (photo.Length > 5 * 1024 * 1024)
                throw new BadRequestException("Photo must be under 5 MB.");

            var uploadDir = Path.Combine(env.WebRootPath ?? "wwwroot", "uploads", "reviews");
            Directory.CreateDirectory(uploadDir);

            var fileName = $"review_{reviewId}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await photo.CopyToAsync(stream);

            // Delete old photo if exists
            if (!string.IsNullOrEmpty(review.PhotoUrl))
            {
                var oldPath = Path.Combine(env.WebRootPath ?? "wwwroot", review.PhotoUrl.TrimStart('/'));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            review.PhotoUrl = $"/uploads/reviews/{fileName}";
            await _reviewRepo.UpdateAsync(reviewId, review);

            // Credit 100 coins to the user's wallet for uploading a photo
            const decimal photoRewardCoins = 100m;
            try
            {
                await _walletService.CreditAsync(
                    review.UserId,
                    photoRewardCoins,
                    $"🎉 Photo review reward for Review #{reviewId}",
                    reviewId);
                _logger.LogInformation("100 coins credited to User:{UserId} for photo review", review.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to credit photo reward for Review:{ReviewId}", reviewId);
            }

            _logger.LogInformation("Photo uploaded for Review:{ReviewId} → {PhotoUrl}", reviewId, review.PhotoUrl);
            var dto = MapToDto(review);
            dto.CoinsEarned = (int)photoRewardCoins;
            return dto;
        }

        // ── DELETE ────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int reviewId)
        {
            _logger.LogInformation("Deleting review {ReviewId}", reviewId);
            var deleted = await _reviewRepo.DeleteAsync(reviewId);
            if (deleted is null) throw new NotFoundException("Review", reviewId);
            await LogAsync("ReviewDeleted", reviewId);
            return true;
        }

        // ── MAPPER ────────────────────────────────────────────────────────
        private static ReviewResponseDto MapToDto(Review r) => new()
        {
            ReviewId  = r.ReviewId,
            HotelId   = r.HotelId,
            UserId    = r.UserId,
            Rating    = r.Rating,
            Comment   = r.Comment,
            PhotoUrl  = r.PhotoUrl,
            CreatedAt = r.CreatedAt
        };
    }
}
