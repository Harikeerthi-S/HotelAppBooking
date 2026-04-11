using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IChatService
    {
        /// <summary>Process a user message and return a bot reply.</summary>
        Task<ChatResponseDto> ReplyAsync(ChatRequestDto request);

        /// <summary>Get full conversation history for a session.</summary>
        Task<IEnumerable<ChatHistoryDto>> GetHistoryAsync(string sessionId);

        /// <summary>Get all sessions for a user.</summary>
        Task<IEnumerable<ChatHistoryDto>> GetByUserAsync(int userId);

        /// <summary>Delete all messages in a session.</summary>
        Task<bool> ClearSessionAsync(string sessionId);
    }
}
