using System.Text.Json;
using MiddleMan.CustomSerializer;
using MiddleMan.Dtos;
using MiddleMan.Services.JsonProvider;

namespace MiddleMan.Services.Booking;

public class BookingService(IJsonProviderService jsonProviderService) : IBookingService
{
    private const string BookingFileName = "booking";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        Converters = { new BookingDtoConverter() }
    };

    public Task<BookingDto> GetBooking()
    {
        var json = jsonProviderService.GetJson(BookingFileName);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Booking data is empty.");
        }

        var booking = JsonSerializer.Deserialize<BookingDto>(json, JsonOptions);
        return Task.FromResult(booking ?? throw new InvalidOperationException("Failed to deserialize booking data."));
    }
}
