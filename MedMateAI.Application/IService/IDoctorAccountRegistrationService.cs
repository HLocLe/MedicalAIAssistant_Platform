namespace MedMateAI.Application.IService;

public interface IDoctorAccountRegistrationService
{
    Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, Guid UserId, IEnumerable<string> Errors)> CreateDoctorUserAsync(
        string email,
        string fullName,
        string password,
        string? phoneNumber,
        CancellationToken cancellationToken = default);
}
