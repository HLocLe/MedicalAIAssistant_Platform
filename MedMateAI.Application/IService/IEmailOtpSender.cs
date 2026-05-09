namespace MedMateAI.Application.IService;

public interface IEmailOtpSender
{
    Task<(bool Success, string? OtpCode)> SendOtpEmailAsync(string toEmail, CancellationToken cancellationToken = default);
}
