using Microsoft.Extensions.Logging;
using Moq;

namespace HotelBookingApp.Tests.Helpers
{
    /// <summary>Creates a NullLogger for any service type.</summary>
    public static class MockLogger
    {
        public static ILogger<T> Create<T>() =>
            new Mock<ILogger<T>>().Object;
    }
}
