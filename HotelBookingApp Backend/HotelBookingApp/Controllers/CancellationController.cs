using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/cancellation")]
    [Authorize]
    public class CancellationController : ControllerBase
    {
        private readonly ICancellationService _cancellationService;

        public CancellationController(ICancellationService cancellationService)
            => _cancellationService = cancellationService;

        [HttpPost]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> Create(CreateCancellationDto dto)
            => Ok(await _cancellationService.CreateAsync(dto));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
            => Ok(await _cancellationService.GetByIdAsync(id));

        [HttpPost("paged")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetAll(PagedRequestDto request)
            => Ok(await _cancellationService.GetAllAsync(request));

        [HttpPost("user/{userId}/paged")]
        public async Task<IActionResult> GetByUser(int userId, PagedRequestDto request)
            => Ok(await _cancellationService.GetByUserAsync(userId, request));

        [HttpPut("{id}/status")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> UpdateStatus(int id, string status, decimal refundAmount = 0)
            => Ok(await _cancellationService.UpdateStatusAsync(id, status, refundAmount));
    }
}
