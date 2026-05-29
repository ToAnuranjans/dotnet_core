using System.Text.Json;
using System.Text.Json.Serialization;
using MiddleMan.Dtos;

namespace MiddleMan.CustomSerializer;

public class BookingDtoConverter : JsonConverter<BookingDto>
{
    public override BookingDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var booking = RequiredObject(document.RootElement, "booking");
        var customer = RequiredObject(booking, "customer");
        var personalInfo = RequiredObject(customer, "personalInfo");
        var contact = RequiredObject(customer, "contact");
        var flight = RequiredObject(RequiredObject(booking, "trip"), "flight");
        var origin = RequiredObject(flight, "origin");
        var destination = RequiredObject(flight, "destination");
        var payment = RequiredObject(booking, "payment");
        var amount = RequiredObject(payment, "amount");

        return new BookingDto
        {
            BookingId = RequiredString(booking, "id"),
            CreatedAt = RequiredDateTime(booking, "createdAt"),
            FirstName = RequiredString(personalInfo, "firstName"),
            LastName = RequiredString(personalInfo, "lastName"),
            Email = RequiredString(contact, "email"),
            Phone = RequiredString(contact, "phone"),
            OriginCode = RequiredString(origin, "code"),
            OriginCity = RequiredString(origin, "city"),
            DestinationCode = RequiredString(destination, "code"),
            DestinationCity = RequiredString(destination, "city"),
            Amount = RequiredDecimal(amount, "value"),
            Currency = RequiredString(amount, "currency"),
            PaymentStatus = RequiredString(payment, "status")
        };
    }

    public override void Write(Utf8JsonWriter writer, BookingDto value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    private static JsonElement RequiredObject(JsonElement element, string propertyName)
    {
        var property = RequiredProperty(element, propertyName);
        if (property.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Booking property '{propertyName}' must be an object.");
        }

        return property;
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        var value = RequiredProperty(element, propertyName).GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException($"Booking property '{propertyName}' is required.");
        }

        return value;
    }

    private static DateTime RequiredDateTime(JsonElement element, string propertyName)
    {
        if (!RequiredProperty(element, propertyName).TryGetDateTime(out var value))
        {
            throw new JsonException($"Booking property '{propertyName}' must be a valid date.");
        }

        return value;
    }

    private static decimal RequiredDecimal(JsonElement element, string propertyName)
    {
        if (!RequiredProperty(element, propertyName).TryGetDecimal(out var value))
        {
            throw new JsonException($"Booking property '{propertyName}' must be a valid decimal.");
        }

        return value;
    }

    private static JsonElement RequiredProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            throw new JsonException($"Booking property '{propertyName}' is missing.");
        }

        return property;
    }
}
