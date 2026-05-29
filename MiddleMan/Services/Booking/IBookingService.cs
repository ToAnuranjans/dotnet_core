using MiddleMan.Dtos;

namespace MiddleMan.Services.Booking;

public interface IBookingService
{
    Task<BookingDto> GetBooking();
}