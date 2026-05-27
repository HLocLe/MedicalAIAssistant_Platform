namespace MedMateAI.Domain.Entities;

public sealed class MedicalDepartment : BaseEntity
{
    public string? DepartmentName { get; set; }

    public string? Description { get; set; }



    public ICollection<FacilityDepartment> FacilityDepartments { get; set; } = new List<FacilityDepartment>();

    public ICollection<DepartmentRecommendation> DepartmentRecommendations { get; set; } = new List<DepartmentRecommendation>();

    public ICollection<ConsultationSession> ConsultationSessions { get; set; } = new List<ConsultationSession>();

    public ICollection<TreatmentJourney> TreatmentJourneys { get; set; } = new List<TreatmentJourney>();
}
