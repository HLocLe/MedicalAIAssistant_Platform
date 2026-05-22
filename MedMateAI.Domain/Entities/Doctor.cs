using MedMateAI.Domain.Enums;

namespace MedMateAI.Domain.Entities;

public sealed class Doctor : BaseEntity
{
    public Guid FacilityDepartmentId { get; set; }

    public string? FullName { get; set; }

    public string? Specialty { get; set; }

    public string? AcademicTitle { get; set; }

    public DepartmentRole DepartmentRole { get; set; } = DepartmentRole.Staff;

    public int? YearsOfExperience { get; set; }

    public bool IsActive { get; set; }

    public FacilityDepartment FacilityDepartment { get; set; } = null!;

    public ICollection<ConsultationSession> ConsultationSessions { get; set; } = new List<ConsultationSession>();

    public ICollection<MedicalVisit> MedicalVisits { get; set; } = new List<MedicalVisit>();

    public ICollection<FeedbackReview> FeedbackReviews { get; set; } = new List<FeedbackReview>();
}
