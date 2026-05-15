using MedMateAI.Domain.Enums;

public sealed class ApproveUserResponse
{
    public Guid UserId { get; set; }
    public UserStatus Status { get; set; }
}