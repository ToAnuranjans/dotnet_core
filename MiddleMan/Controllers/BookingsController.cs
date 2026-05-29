using Microsoft.AspNetCore.Mvc;
using MiddleMan.Services.Booking;
namespace MiddleMan.Controllers;

[Route("[controller]")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBookings()
    {
        var booking = await bookingService.GetBooking();
        return Ok(booking);
    }
}