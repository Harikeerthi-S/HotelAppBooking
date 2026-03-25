namespace HotelBookingApp.Interfaces.IServices
{
    public interface IPasswordService
    {
        byte[] HashPassword(string password);
        bool VerifyPassword(string password, byte[] storedHash);
    }
}
