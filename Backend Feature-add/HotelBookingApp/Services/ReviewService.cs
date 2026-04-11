using HotelBookingApp.Delegates;
using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

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

        private void Log(string action, int? entityId, int? userId = null, string? changes = null)
            => _ = _audit.CreateAsync(new CreateAuditLogDto
            {
                UserId = userId, Action = action, EntityName = "Review",
                EntityId = entityId, Changes = changes
            });

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

            var alreadyReviewed = await _reviewRepo.ExistsAsync(
                r => r.HotelId == dto.HotelId && r.UserId == dto.UserId
            );
            if (alreadyReviewed)
                throw new AlreadyExistsException("You have already reviewed this hotel.");

            var photoUrl = dto.PhotoUrl?.Trim();

            var review = new Review
            {
                HotelId   = dto.HotelId,
                UserId    = dto.UserId,
                Rating    = dto.Rating,
                Comment   = dto.Comment.Trim(),
                PhotoUrl  = photoUrl,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _reviewRepo.AddAsync(review);
            _logger.LogInformation("Review created: {ReviewId}", created.ReviewId);

            int coinsEarned = 0;

            // Auto-credit 100 coins if photo was included at creation time
            if (!string.IsNullOrWhiteSpace(photoUrl))
            {
                coinsEarned = (int)AppDelegateFactory.PhotoReviewCredits;
                await CreditPhotoCoinsAsync(dto.UserId, created.ReviewId, coinsEarned);
            }

            Log("ReviewCreated", created.ReviewId, dto.UserId,
                $"Hotel:{dto.HotelId} Rating:{dto.Rating}/5 Photo:{(photoUrl != null ? "yes" : "no")}");

            return MapToDto(created, coinsEarned);
        }

        // ── UPLOAD PHOTO ──────────────────────────────────────────────────
        public async Task<ReviewResponseDto> UploadPhotoAsync(int reviewId, string photoUrl)
        {
            if (string.IsNullOrWhiteSpace(photoUrl))
                throw new BadRequestException("Photo URL cannot be empty.");

            var review = await _reviewRepo.GetByIdAsync(reviewId)
                         ?? throw new NotFoundException("Review", reviewId);

            // Idempotent — don't credit twice
            if (!string.IsNullOrWhiteSpace(review.PhotoUrl))
                throw new BadRequestException("This review already has a photo. Credits were already awarded.");

            review.PhotoUrl = photoUrl.Trim();
            await _reviewRepo.UpdateAsync(reviewId, review);

            var coinsEarned = (int)AppDelegateFactory.PhotoReviewCredits;
            await CreditPhotoCoinsAsync(review.UserId, reviewId, coinsEarned);

            Log("ReviewPhotoUploaded", reviewId, review.UserId,
                $"Photo added — ₹{coinsEarned} credits awarded");

            _logger.LogInformation("Photo uploaded for Review:{ReviewId}, {Coins} credits → User:{UserId}",
                reviewId, coinsEarned, review.UserId);

            return MapToDto(review, coinsEarned);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────
        public async Task<ReviewResponseDto?> GetByIdAsync(int reviewId)
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId)
                         ?? throw new NotFoundException("Review", reviewId);
            return MapToDto(review);
        }

        // ── GET PAGED ─────────────────────────────────────────────────────
        public async Task<PagedResponseDto<ReviewResponseDto>> GetPagedAsync(
            ReviewFilterDto filter, PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 10);

            var all   = await _reviewRepo.GetAllAsync();
            var query = all.AsQueryable();

            if (filter.HotelId.HasValue) query = query.Where(r => r.HotelId == filter.HotelId.Value);
            if (filter.UserId.HasValue)  query = query.Where(r => r.UserId  == filter.UserId.Value);
            if (filter.Rating.HasValue)  query = query.Where(r => r.Rating  == filter.Rating.Value);

            var ordered = query.OrderByDescending(r => r.CreatedAt);
            var total   = ordered.Count();
            var data    = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => MapToDto(r))
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

        // ── DELETE ────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int reviewId)
        {
            _logger.LogInformation("Deleting review {ReviewId}", reviewId);
            var deleted = await _reviewRepo.DeleteAsync(reviewId);
            if (deleted is null) throw new NotFoundException("Review", reviewId);
            Log("ReviewDeleted", reviewId);
            return true;
        }

        // ── PRIVATE ───────────────────────────────────────────────────────
        private async Task CreditPhotoCoinsAsync(int userId, int reviewId, int coins)
        {
            try
            {
                await _walletService.CreditAsync(
                    userId:      userId,
                    amount:      coins,
                    description: $"📸 Photo review bonus — Review #{reviewId}",
                    referenceId: reviewId
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not credit photo coins for Review:{ReviewId} User:{UserId}", reviewId, userId);
            }
        }

        private static ReviewResponseDto MapToDto(Review r, int coinsEarned = 0) => new()
        {
            ReviewId    = r.ReviewId,
            HotelId     = r.HotelId,
            UserId      = r.UserId,
            Rating      = r.Rating,
            Comment     = r.Comment,
            PhotoUrl    = r.PhotoUrl,
            CoinsEarned = coinsEarned,
            CreatedAt   = r.CreatedAt
        };
    }
}
