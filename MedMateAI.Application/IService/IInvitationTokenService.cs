namespace MedMateAI.Application.IService;

public interface IInvitationTokenService
{
    string GenerateToken();

    string HashToken(string rawToken);
}
