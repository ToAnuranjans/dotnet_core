using System.Text.Json;
using System.Text.Json.Serialization;
using MiddleMan.Serialization;

namespace MiddleMan.Tests.Serialization;

public class JsonPropertyPathConverterTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonPropertyPathConverterFactory() }
    };

    [Fact]
    public void Deserialize_MapsScalarValuesFromAbsolutePaths()
    {
        const string json = """
        {
          "booking": {
            "id": "BK12345",
            "createdAt": "2026-05-29T10:30:00Z",
            "customer": {
              "personalInfo": {
                "firstName": "Anuranjan",
                "lastName": "Srivastava"
              }
            },
            "payment": {
              "amount": {
                "value": 12500.75
              }
            }
          }
        }
        """;

        var result = JsonSerializer.Deserialize<ScalarProjection>(json, Options);

        Assert.NotNull(result);
        Assert.Equal("BK12345", result.BookingId);
        Assert.Equal(new DateTime(2026, 5, 29, 10, 30, 0, DateTimeKind.Utc), result.CreatedAt);
        Assert.Equal("Anuranjan", result.FirstName);
        Assert.Equal("Srivastava", result.LastName);
        Assert.Equal(12500.75m, result.Amount);
    }

    [Fact]
    public void Deserialize_ResolvesRelativePathsAgainstSingleRootObject()
    {
        const string json = """
        {
          "booking": {
            "customer": {
              "personalInfo": {
                "firstName": "Anuranjan"
              }
            }
          }
        }
        """;

        var result = JsonSerializer.Deserialize<RelativePathProjection>(json, Options);

        Assert.NotNull(result);
        Assert.Equal("Anuranjan", result.FirstName);
    }

    [Fact]
    public void Deserialize_LeavesMissingOptionalNullablePropertiesAsNull()
    {
        const string json = """{ "booking": { "id": "BK12345" } }""";

        var result = JsonSerializer.Deserialize<OptionalProjection>(json, Options);

        Assert.NotNull(result);
        Assert.Null(result.MissingString);
    }

    [Fact]
    public void Deserialize_UsesDefaultValueForMissingOptionalValueTypes()
    {
        const string json = """{ "booking": { "id": "BK12345" } }""";

        var result = JsonSerializer.Deserialize<OptionalValueTypeProjection>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(0, result.MissingNumber);
    }

    [Fact]
    public void Deserialize_ThrowsForMissingRequiredPath()
    {
        const string json = """{ "booking": { "id": "BK12345" } }""";

        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<RequiredProjection>(json, Options));

        Assert.Contains("booking.customer.email", exception.Message);
    }

    [Fact]
    public void Deserialize_ComposesStringFromMultiplePaths()
    {
        const string json = """
        {
          "address": {
            "street": "123 Main Street",
            "city": "Pune",
            "postalCode": "411001",
            "country": "India"
          }
        }
        """;

        var result = JsonSerializer.Deserialize<CompositeProjection>(json, Options);

        Assert.NotNull(result);
        Assert.Equal("123 Main Street | Pune | 411001 | India", result.CompleteAddress);
    }

    [Fact]
    public void Deserialize_ThrowsWhenRequiredCompositeComponentIsMissing()
    {
        const string json = """
        {
          "address": {
            "street": "123 Main Street",
            "city": "Pune"
          }
        }
        """;

        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<RequiredCompositeProjection>(json, Options));

        Assert.Contains("address.postalCode", exception.Message);
    }

    [Fact]
    public void Deserialize_MapsCollectionOfComplexItemsWithConventionAndCompositeProperties()
    {
        const string json = """
        {
          "booking": {
            "addresses": [
              {
                "type": "Billing",
                "street": "123 Main Street",
                "city": "Pune",
                "postalCode": "411001",
                "country": "India"
              },
              {
                "type": "Shipping",
                "street": "456 Elm Street",
                "city": "Delhi",
                "postalCode": "110001",
                "country": "India"
              }
            ]
          }
        }
        """;

        var result = JsonSerializer.Deserialize<AddressCollectionProjection>(json, Options);

        Assert.NotNull(result);
        Assert.NotNull(result.Addresses);
        Assert.Collection(
            result.Addresses,
            address =>
            {
                Assert.Equal("Billing", address.Type);
                Assert.Equal("123 Main Street, Pune, 411001, India", address.CompleteAddress);
            },
            address =>
            {
                Assert.Equal("Shipping", address.Type);
                Assert.Equal("456 Elm Street, Delhi, 110001, India", address.CompleteAddress);
            });
    }

    [Fact]
    public void Deserialize_MapsPrimitiveListFromArrayItemPath()
    {
        const string json = """
        {
          "payload": {
            "tags": [
              { "name": "vip" },
              { "name": "priority" }
            ]
          }
        }
        """;

        var result = JsonSerializer.Deserialize<PrimitiveCollectionProjection>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(["vip", "priority"], result.TagNames);
    }

    [Fact]
    public void Deserialize_MapsPrimitiveCollectionFromArrayWildcardWithoutItemPath()
    {
        const string json = """
        {
          "payload": {
            "codes": [101, 202, 303]
          }
        }
        """;

        var result = JsonSerializer.Deserialize<PrimitiveArrayWildcardProjection>(json, Options);

        Assert.NotNull(result);
        Assert.Equal([101, 202, 303], result.Codes);
    }

    [Fact]
    public void Deserialize_MapsNonListCollectionTypeFromArrayWildcard()
    {
        const string json = """
        {
          "payload": {
            "codes": ["A", "B", "A"]
          }
        }
        """;

        var result = JsonSerializer.Deserialize<HashSetProjection>(json, Options);

        Assert.NotNull(result);
        Assert.NotNull(result.Codes);
        Assert.Equal(["A", "B"], result.Codes.Order());
    }

    [Fact]
    public void Deserialize_MapsArrayPropertyFromArrayItemPath()
    {
        const string json = """
        {
          "payload": {
            "tags": [
              { "name": "vip" },
              { "name": "priority" }
            ]
          }
        }
        """;

        var result = JsonSerializer.Deserialize<ArrayProjection>(json, Options);

        Assert.NotNull(result);
        Assert.NotNull(result.TagNames);
        Assert.Equal(["vip", "priority"], result.TagNames);
    }

    [Fact]
    public void Deserialize_ThrowsWhenCollectionPathDoesNotResolveToArray()
    {
        const string json = """
        {
          "payload": {
            "tags": { "name": "vip" }
          }
        }
        """;

        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<PrimitiveCollectionProjection>(json, Options));

        Assert.Contains("payload.tags", exception.Message);
        Assert.Contains("must be an array", exception.Message);
    }

    [Fact]
    public void Deserialize_LeavesMissingOptionalCollectionAsNull()
    {
        const string json = """{ "payload": { } }""";

        var result = JsonSerializer.Deserialize<PrimitiveCollectionProjection>(json, Options);

        Assert.NotNull(result);
        Assert.Null(result.TagNames);
    }

    [Fact]
    public void Deserialize_ThrowsWhenRequiredCollectionPathIsMissing()
    {
        const string json = """{ "payload": { } }""";

        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<RequiredCollectionProjection>(json, Options));

        Assert.Contains("payload.tags", exception.Message);
    }

    [Fact]
    public void Deserialize_ThrowsWhenCollectionAttributeDoesNotUseWildcard()
    {
        const string json = """{ "payload": { "tags": [] } }""";

        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CollectionWithoutWildcardProjection>(json, Options));

        Assert.Contains("must contain '[n]'", exception.Message);
    }

    [Fact]
    public void Deserialize_MapsDictionaryAndDictionaryKeyPath()
    {
        const string json = """
        {
          "payload": {
            "metadata": {
              "source": "web",
              "campaign": "summer"
            }
          }
        }
        """;

        var result = JsonSerializer.Deserialize<DictionaryProjection>(json, Options);

        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal("web", result.Metadata["source"]);
        Assert.Equal("summer", result.Metadata["campaign"]);
        Assert.Equal("web", result.Source);
    }

    [Fact]
    public void Deserialize_MapsArrayIndexPath()
    {
        const string json = """
        {
          "payload": {
            "addresses": [
              { "city": "Pune" },
              { "city": "Delhi" }
            ]
          }
        }
        """;

        var result = JsonSerializer.Deserialize<ArrayIndexProjection>(json, Options);

        Assert.NotNull(result);
        Assert.Equal("Delhi", result.SecondCity);
    }

    [Fact]
    public void Deserialize_ThrowsForMalformedIndexerPath()
    {
        const string json = """{ "payload": { "addresses": [] } }""";

        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MalformedPathProjection>(json, Options));

        Assert.Contains("unterminated indexer", exception.Message);
    }

    [Fact]
    public void Attribute_ThrowsWhenPathIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() => new JsonPropertyPathAttribute(" "));

        Assert.Equal("path", exception.ParamName);
    }

    [Fact]
    public void Deserialize_MapsNestedComplexTypeWithAttributedAndConventionProperties()
    {
        const string json = """
        {
          "payload": {
            "customer": {
              "profile": {
                "first": "Anuranjan"
              },
              "city": "Pune"
            }
          }
        }
        """;

        var result = JsonSerializer.Deserialize<NestedComplexProjection>(json, Options);

        Assert.NotNull(result);
        Assert.NotNull(result.Customer);
        Assert.Equal("Anuranjan", result.Customer.FirstName);
        Assert.Equal("Pune", result.Customer.City);
    }

    [Fact]
    public void Deserialize_UsesJsonPropertyNameForConventionReadInsideProjectedItems()
    {
        const string json = """
        {
          "payload": {
            "items": [
              { "item_type": "primary" }
            ]
          }
        }
        """;

        var result = JsonSerializer.Deserialize<JsonPropertyNameProjection>(json, Options);

        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items);
        Assert.Equal("primary", result.Items[0].Type);
    }

    [Fact]
    public void Deserialize_MapsOrderPayloadWithBooleansNullableNumbersEnumsAndDateTimeOffset()
    {
        const string json = """
        {
          "order": {
            "id": "ORD-1001",
            "placedAt": "2026-01-15T13:45:30+05:30",
            "status": "Paid",
            "flags": {
              "expedited": true
            },
            "discount": {
              "percentage": 12.5
            },
            "customer": {
              "tier": null
            }
          }
        }
        """;

        var result = JsonSerializer.Deserialize<OrderProjection>(json, Options);

        Assert.NotNull(result);
        Assert.Equal("ORD-1001", result.OrderId);
        Assert.Equal(DateTimeOffset.Parse("2026-01-15T13:45:30+05:30"), result.PlacedAt);
        Assert.Equal(OrderStatus.Paid, result.Status);
        Assert.True(result.Expedited);
        Assert.Equal(12.5m, result.DiscountPercentage);
        Assert.Null(result.CustomerTier);
    }

    [Fact]
    public void Deserialize_MapsSurveyPayloadWithNestedCollectionsAndOptionalItemFields()
    {
        const string json = """
        {
          "survey": {
            "responses": [
              {
                "question": {
                  "id": "Q1",
                  "text": "Cleanliness"
                },
                "answer": {
                  "score": 5,
                  "comment": "Excellent"
                }
              },
              {
                "question": {
                  "id": "Q2",
                  "text": "Queue time"
                },
                "answer": {
                  "score": 3
                }
              }
            ]
          }
        }
        """;

        var result = JsonSerializer.Deserialize<SurveyProjection>(json, Options);

        Assert.NotNull(result);
        Assert.NotNull(result.Responses);
        Assert.Collection(
            result.Responses,
            response =>
            {
                Assert.Equal("Q1", response.QuestionId);
                Assert.Equal("Cleanliness", response.QuestionText);
                Assert.Equal(5, response.Score);
                Assert.Equal("Excellent", response.Comment);
            },
            response =>
            {
                Assert.Equal("Q2", response.QuestionId);
                Assert.Equal("Queue time", response.QuestionText);
                Assert.Equal(3, response.Score);
                Assert.Null(response.Comment);
            });
    }

    [Fact]
    public void Deserialize_MapsDictionaryOfComplexValues()
    {
        const string json = """
        {
          "inventory": {
            "warehouses": {
              "PNQ": {
                "available": 10,
                "reserved": 2
              },
              "DEL": {
                "available": 7,
                "reserved": 1
              }
            }
          }
        }
        """;

        var result = JsonSerializer.Deserialize<InventoryProjection>(json, Options);

        Assert.NotNull(result);
        Assert.NotNull(result.Warehouses);
        Assert.Equal(10, result.Warehouses["PNQ"].Available);
        Assert.Equal(2, result.Warehouses["PNQ"].Reserved);
        Assert.Equal(7, result.Warehouses["DEL"].Available);
        Assert.Equal(1, result.Warehouses["DEL"].Reserved);
    }

    [Fact]
    public void Deserialize_MapsComplexArrayItemPath()
    {
        const string json = """
        {
          "audit": {
            "events": [
              {
                "actor": {
                  "profile": {
                    "id": "U001",
                    "displayName": "Anu"
                  }
                }
              },
              {
                "actor": {
                  "profile": {
                    "id": "U002",
                    "displayName": "Reviewer"
                  }
                }
              }
            ]
          }
        }
        """;

        var result = JsonSerializer.Deserialize<AuditProjection>(json, Options);

        Assert.NotNull(result);
        Assert.NotNull(result.Actors);
        Assert.Collection(
            result.Actors,
            actor =>
            {
                Assert.Equal("U001", actor.Id);
                Assert.Equal("Anu", actor.DisplayName);
            },
            actor =>
            {
                Assert.Equal("U002", actor.Id);
                Assert.Equal("Reviewer", actor.DisplayName);
            });
    }

    [Fact]
    public void Deserialize_SkipsMissingOptionalArrayItemPathValues()
    {
        const string json = """
        {
          "payload": {
            "tags": [
              { "name": "vip" },
              { "label": "missing-name" },
              { "name": "priority" }
            ]
          }
        }
        """;

        var result = JsonSerializer.Deserialize<PrimitiveCollectionProjection>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(["vip", "priority"], result.TagNames);
    }

    [Fact]
    public void Deserialize_ThrowsWhenRequiredArrayItemPathIsMissing()
    {
        const string json = """
        {
          "payload": {
            "tags": [
              { "name": "vip" },
              { "label": "missing-name" }
            ]
          }
        }
        """;

        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<RequiredCollectionProjection>(json, Options));

        Assert.Contains("payload.tags[n].name", exception.Message);
    }

    [Fact]
    public void Deserialize_IgnoresConverterFactoryForTypesWithoutPathAttributes()
    {
        const string json = """
        {
          "id": "plain-1",
          "count": 42
        }
        """;

        var result = JsonSerializer.Deserialize<PlainProjection>(json, Options);

        Assert.NotNull(result);
        Assert.Equal("plain-1", result.Id);
        Assert.Equal(42, result.Count);
    }

    [Fact]
    public void Serialize_UsesNormalSystemTextJsonSerializationAndJsonPropertyName()
    {
        var value = new ScalarProjection
        {
            BookingId = "BK12345",
            FirstName = "Anuranjan",
            LastName = "Srivastava",
            Amount = 100m
        };

        var json = JsonSerializer.Serialize(value, Options);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.TryGetProperty("bookingId", out var bookingId));
        Assert.Equal("BK12345", bookingId.GetString());
        Assert.True(document.RootElement.TryGetProperty("first_name", out var firstName));
        Assert.Equal("Anuranjan", firstName.GetString());
        Assert.False(document.RootElement.TryGetProperty("booking", out _));
    }

    private sealed class ScalarProjection
    {
        [JsonPropertyPath("booking.id")]
        public string? BookingId { get; set; }

        [JsonPropertyPath("booking.createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyPath("booking.customer.personalInfo.firstName")]
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyPath("booking.customer.personalInfo.lastName")]
        public string? LastName { get; set; }

        [JsonPropertyPath("booking.payment.amount.value")]
        public decimal Amount { get; set; }
    }

    private sealed class RelativePathProjection
    {
        [JsonPropertyPath("customer.personalInfo.firstName")]
        public string? FirstName { get; set; }
    }

    private sealed class OptionalProjection
    {
        [JsonPropertyPath("booking.customer.email")]
        public string? MissingString { get; set; }
    }

    private sealed class OptionalValueTypeProjection
    {
        [JsonPropertyPath("booking.missingNumber")]
        public int MissingNumber { get; set; }
    }

    private sealed class RequiredProjection
    {
        [JsonPropertyPath("booking.customer.email", Required = true)]
        public string? Email { get; set; }
    }

    private sealed class CompositeProjection
    {
        [JsonPropertyPath("address.street", "address.city", "address.postalCode", "address.country", Separator = " | ")]
        public string? CompleteAddress { get; set; }
    }

    private sealed class RequiredCompositeProjection
    {
        [JsonPropertyPath("address.street", "address.city", "address.postalCode", Required = true)]
        public string? CompleteAddress { get; set; }
    }

    private sealed class AddressCollectionProjection
    {
        [JsonPropertyPath("booking.addresses[n]")]
        public List<AddressItem>? Addresses { get; set; }
    }

    private sealed class AddressItem
    {
        public string? Type { get; set; }

        [JsonPropertyPath("street", "city", "postalCode", "country")]
        public string? CompleteAddress { get; set; }
    }

    private sealed class PrimitiveCollectionProjection
    {
        [JsonPropertyPath("payload.tags[n].name")]
        public List<string>? TagNames { get; set; }
    }

    private sealed class PrimitiveArrayWildcardProjection
    {
        [JsonPropertyPath("payload.codes[n]")]
        public List<int>? Codes { get; set; }
    }

    private sealed class HashSetProjection
    {
        [JsonPropertyPath("payload.codes[n]")]
        public HashSet<string>? Codes { get; set; }
    }

    private sealed class RequiredCollectionProjection
    {
        [JsonPropertyPath("payload.tags[n].name", Required = true)]
        public List<string>? TagNames { get; set; }
    }

    private sealed class CollectionWithoutWildcardProjection
    {
        [JsonPropertyPath("payload.tags")]
        public List<string>? TagNames { get; set; }
    }

    private sealed class ArrayProjection
    {
        [JsonPropertyPath("payload.tags[n].name")]
        public string[]? TagNames { get; set; }
    }

    private sealed class DictionaryProjection
    {
        [JsonPropertyPath("payload.metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        [JsonPropertyPath("payload.metadata[\"source\"]")]
        public string? Source { get; set; }
    }

    private sealed class ArrayIndexProjection
    {
        [JsonPropertyPath("payload.addresses[1].city")]
        public string? SecondCity { get; set; }
    }

    private sealed class MalformedPathProjection
    {
        [JsonPropertyPath("payload.addresses[0.city")]
        public string? City { get; set; }
    }

    private sealed class NestedComplexProjection
    {
        [JsonPropertyPath("payload.customer")]
        public CustomerProjection? Customer { get; set; }
    }

    private sealed class CustomerProjection
    {
        [JsonPropertyPath("profile.first")]
        public string? FirstName { get; set; }

        public string? City { get; set; }
    }

    private sealed class JsonPropertyNameProjection
    {
        [JsonPropertyPath("payload.items[n]")]
        public List<JsonPropertyNameItem>? Items { get; set; }
    }

    private sealed class JsonPropertyNameItem
    {
        [JsonPropertyName("item_type")]
        public string? Type { get; set; }
    }

    private sealed class OrderProjection
    {
        [JsonPropertyPath("order.id")]
        public string? OrderId { get; set; }

        [JsonPropertyPath("order.placedAt")]
        public DateTimeOffset PlacedAt { get; set; }

        [JsonPropertyPath("order.status")]
        public OrderStatus Status { get; set; }

        [JsonPropertyPath("order.flags.expedited")]
        public bool Expedited { get; set; }

        [JsonPropertyPath("order.discount.percentage")]
        public decimal? DiscountPercentage { get; set; }

        [JsonPropertyPath("order.customer.tier")]
        public string? CustomerTier { get; set; }
    }

    private enum OrderStatus
    {
        Pending,
        Paid,
        Cancelled
    }

    private sealed class SurveyProjection
    {
        [JsonPropertyPath("survey.responses[n]")]
        public List<SurveyResponseProjection>? Responses { get; set; }
    }

    private sealed class SurveyResponseProjection
    {
        [JsonPropertyPath("question.id")]
        public string? QuestionId { get; set; }

        [JsonPropertyPath("question.text")]
        public string? QuestionText { get; set; }

        [JsonPropertyPath("answer.score")]
        public int Score { get; set; }

        [JsonPropertyPath("answer.comment")]
        public string? Comment { get; set; }
    }

    private sealed class InventoryProjection
    {
        [JsonPropertyPath("inventory.warehouses")]
        public Dictionary<string, StockProjection>? Warehouses { get; set; }
    }

    private sealed class StockProjection
    {
        public int Available { get; set; }

        public int Reserved { get; set; }
    }

    private sealed class AuditProjection
    {
        [JsonPropertyPath("audit.events[n].actor.profile")]
        public List<ActorProjection>? Actors { get; set; }
    }

    private sealed class ActorProjection
    {
        public string? Id { get; set; }

        public string? DisplayName { get; set; }
    }

    private sealed class PlainProjection
    {
        public string? Id { get; set; }

        public int Count { get; set; }
    }
}
