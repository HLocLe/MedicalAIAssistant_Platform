using System.Text.Json.Serialization;

namespace MedMateAI.Infrastructure.Email.Brevo.Models;

internal sealed class BrevoRecipient
{
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;
}
