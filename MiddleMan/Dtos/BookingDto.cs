namespace MiddleMan.Dtos;

public class BookingDto
{
    public string? BookingId { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string? OriginCode { get; set; }
    public string? OriginCity { get; set; }

    public string? DestinationCode { get; set; }
    public string? DestinationCity { get; set; }

    public decimal Amount { get; set; }
    public string? Currency { get; set; }

    public string? PaymentStatus { get; set; }
}