using System.Text.Json;
using System.Text.Json.Serialization;
using HotelResortBooking.Api.Contracts.Bookings;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Api.Serialization;

public sealed class FlexiblePaymentMethodJsonConverter : JsonConverter<PaymentMethodType>
{
    public override PaymentMethodType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numeric))
        {
            if (Enum.IsDefined(typeof(PaymentMethodType), numeric))
            {
                return (PaymentMethodType)numeric;
            }

            throw new JsonException($"Unknown payment method value '{numeric}'.");
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            if (TryParse(reader.GetString(), out var method))
            {
                return method;
            }

            throw new JsonException($"Unknown payment method '{reader.GetString()}'.");
        }

        if (reader.TokenType is JsonTokenType.Null)
        {
            return PaymentMethodType.CreditCard;
        }

        throw new JsonException("Payment method must be a string or number.");
    }

    public override void Write(Utf8JsonWriter writer, PaymentMethodType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());

    public static bool TryParse(string? value, out PaymentMethodType method)
    {
        method = PaymentMethodType.CreditCard;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var raw = value.Trim();
        var member = raw;
        var dot = raw.LastIndexOf('.');
        if (dot >= 0 && dot < raw.Length - 1)
        {
            member = raw[(dot + 1)..];
        }

        var normalized = new string(member.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        var fullNormalized = new string(raw.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        const string dartPrefix = "paymentmethodtype";
        if (fullNormalized.StartsWith(dartPrefix, StringComparison.Ordinal)
            && fullNormalized.Length > dartPrefix.Length)
        {
            normalized = fullNormalized[dartPrefix.Length..];
        }

        if (normalized is "creditcard" or "card" or "credit" or "debit" or "creditdebitcard"
            or "creditdebit" or "visa" or "mastercard")
        {
            method = PaymentMethodType.CreditCard;
            return true;
        }

        if (normalized is "payathotel" or "payatproperty" or "hotel" or "cash" or "paylater")
        {
            method = PaymentMethodType.PayAtHotel;
            return true;
        }

        if (normalized is "mobilewallet" or "wallet" or "applepay" or "googlepay")
        {
            method = PaymentMethodType.MobileWallet;
            return true;
        }

        if (normalized is "loyaltypoints" or "points")
        {
            method = PaymentMethodType.LoyaltyPoints;
            return true;
        }

        if (normalized is "pointsandcreditcard" or "pointsandcard")
        {
            method = PaymentMethodType.PointsAndCreditCard;
            return true;
        }

        return Enum.TryParse(member, ignoreCase: true, out method)
               || Enum.TryParse(raw, ignoreCase: true, out method);
    }
}

public sealed class FlexibleNullablePaymentMethodJsonConverter : JsonConverter<PaymentMethodType?>
{
    private static readonly FlexiblePaymentMethodJsonConverter Inner = new();

    public override PaymentMethodType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return null;
        }

        return Inner.Read(ref reader, typeof(PaymentMethodType), options);
    }

    public override void Write(Utf8JsonWriter writer, PaymentMethodType? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        Inner.Write(writer, value.Value, options);
    }
}

public sealed class FlexibleCheckoutPaymentJsonConverter : JsonConverter<BookingCheckoutPaymentApiRequest>
{
    public override BookingCheckoutPaymentApiRequest Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return new BookingCheckoutPaymentApiRequest();
        }

        if (reader.TokenType is JsonTokenType.String or JsonTokenType.Number)
        {
            var method = new FlexiblePaymentMethodJsonConverter().Read(ref reader, typeof(PaymentMethodType), options);
            return new BookingCheckoutPaymentApiRequest { Method = method };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Payment must be an object, string, or number.");
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        return new BookingCheckoutPaymentApiRequest
        {
            Method = ReadMethod(root, "method", "type", "paymentMethod", "paymentMethodType"),
            PaymentMethodToken = ReadString(root, "paymentMethodToken"),
            CardBrand = ReadString(root, "cardBrand"),
            CardLast4 = ReadString(root, "cardLast4"),
            CardholderName = ReadString(root, "cardholderName"),
            IdempotencyKey = ReadString(root, "idempotencyKey")
        };
    }

    public override void Write(Utf8JsonWriter writer, BookingCheckoutPaymentApiRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.Method is { } method)
        {
            writer.WriteString("method", method.ToString());
        }

        WriteString(writer, "paymentMethodToken", value.PaymentMethodToken);
        WriteString(writer, "cardBrand", value.CardBrand);
        WriteString(writer, "cardLast4", value.CardLast4);
        WriteString(writer, "cardholderName", value.CardholderName);
        WriteString(writer, "idempotencyKey", value.IdempotencyKey);
        writer.WriteEndObject();
    }

    private static PaymentMethodType? ReadMethod(JsonElement root, params string[] names)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (!names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.String
                && FlexiblePaymentMethodJsonConverter.TryParse(property.Value.GetString(), out var fromString))
            {
                return fromString;
            }

            if (property.Value.ValueKind == JsonValueKind.Number
                && property.Value.TryGetInt32(out var numeric)
                && Enum.IsDefined(typeof(PaymentMethodType), numeric))
            {
                return (PaymentMethodType)numeric;
            }
        }

        return null;
    }

    private static string? ReadString(JsonElement root, string name)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private static void WriteString(Utf8JsonWriter writer, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            writer.WriteString(name, value);
        }
    }
}
