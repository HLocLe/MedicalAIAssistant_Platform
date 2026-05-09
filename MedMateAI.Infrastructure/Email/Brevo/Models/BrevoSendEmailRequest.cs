using System.Text.Json.Serialization;

namespace MedMateAI.Infrastructure.Email.Brevo.Models;

internal sealed class BrevoSendEmailRequest
{
    [JsonPropertyName("sender")]
    public BrevoSender Sender { get; init; } = null!;

    [JsonPropertyName("to")]
    public BrevoRecipient[] To { get; init; } = [];

    [JsonPropertyName("subject")]
    public string Subject { get; init; } = string.Empty;

    [JsonPropertyName("htmlContent")]
    public string HtmlContent { get; init; } = string.Empty;
}
