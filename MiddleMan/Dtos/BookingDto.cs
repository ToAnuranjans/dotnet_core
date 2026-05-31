using System.Text.Json.Serialization;
using MiddleMan.Serialization;

namespace MiddleMan.Dtos;

public class BookingDto
{
    [JsonPropertyPath("booking.id")]
    [JsonPropertyName("id")]
    public string? BookingId { get; set; }

    [JsonPropertyPath("booking.createdAt")]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyPath("booking.customer.personalInfo.firstName")]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyPath("booking.customer.personalInfo.lastName")]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyPath("booking.customer.contact.email")]
    public string? Email { get; set; }

    [JsonPropertyPath("booking.customer.contact.phone")]
    public string? Phone { get; set; }

    [JsonPropertyPath("booking.trip.flight.origin.code")]
    public string? OriginCode { get; set; }

    [JsonPropertyPath("booking.trip.flight.origin.city")]
    public string? OriginCity { get; set; }

    [JsonPropertyPath("booking.trip.flight.destination.code")]
    public string? DestinationCode { get; set; }

    [JsonPropertyPath("booking.trip.flight.destination.city")]
    public string? DestinationCity { get; set; }

    [JsonPropertyPath("booking.payment.amount.value")]
    public decimal Amount { get; set; }

    [JsonPropertyPath("booking.payment.amount.currency")]
    public string? Currency { get; set; }

    [JsonPropertyPath("booking.payment.status")]
    public string? PaymentStatus { get; set; }

    [JsonPropertyPath("booking.addresses[n]")]
    public List<AddressDto>? Addresses { get; set; }

    [JsonPropertyPath("booking.a.b.c")]
    [JsonPropertyName("testValue")]
    public string? NestedValue { get; set; }
}

public class AddressDto
{
    public string? Type { get; set; }
    [JsonPropertyPath("street", "city", "postalCode", "country")]
    public string? CompleteAddress { get; set; }
}
