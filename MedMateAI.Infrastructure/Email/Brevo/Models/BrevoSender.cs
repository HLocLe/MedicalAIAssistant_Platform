using System.Text.Json.Serialization;

namespace MedMateAI.Infrastructure.Email.Brevo.Models;

internal sealed class BrevoSender
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;
}
