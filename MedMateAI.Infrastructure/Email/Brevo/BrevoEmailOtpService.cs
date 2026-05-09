using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MedMateAI.Application.IService;
using MedMateAI.Infrastructure.Auth.Providers;
using MedMateAI.Infrastructure.Email.Brevo.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MedMateAI.Infrastructure.Email.Brevo;

public sealed class BrevoEmailOtpService : IEmailOtpSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoEmailOtpService> _logger;

    public BrevoEmailOtpService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BrevoEmailOtpService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Success, string? OtpCode)> SendOtpEmailAsync(string toEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return (false, null);
        }

        var apiKey = _configuration["Brevo:ApiKey"];
        var senderEmail = _configuration["Brevo:SenderEmail"];
        var senderName = _configuration["Brevo:SenderName"];

        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(senderEmail) ||
            string.IsNullOrWhiteSpace(senderName))
        {
            _logger.LogError("Brevo configuration is missing (ApiKey, SenderEmail, or SenderName).");
            return (false, null);
        }

        var otpCode = OtpCodeGenerator.CreateNumeric(6);

        var payload = new BrevoSendEmailRequest
        {
            Sender = new BrevoSender { Name = senderName, Email = senderEmail },
            To = [new BrevoRecipient { Email = toEmail.Trim() }],
            Subject = "Mã OTP xác thực tài khoản",
            HtmlContent =
                $"""
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee;'>
                    <h2>Xác thực tài khoản</h2>
                    <p>Mã OTP của bạn là: <strong style='font-size: 24px; color: #2d89ef;'>{otpCode}</strong></p>
                    <p>Mã này sẽ hết hạn sau 1 phút. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
                </div>
                """,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email");
        request.Headers.TryAddWithoutValidation("api-key", apiKey);
        request.Content = JsonContent.Create(payload, options: new()
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (true, otpCode);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Brevo SMTP API failed: {StatusCode} {Body}", response.StatusCode, body);
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo SMTP API request failed.");
            return (false, null);
        }
    }
}
