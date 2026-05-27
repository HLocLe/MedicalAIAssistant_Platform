using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMateAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class refacorlatestdataupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AISystemConfig_DrugAnalysis_DrugAnalysisId",
                table: "AISystemConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_AISystemConfig_MedicalVisit_VisitId",
                table: "AISystemConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_AISystemConfig_MedicationScan_MedicationScanId",
                table: "AISystemConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_FollowUpReminder_TreatmentJourney_TreatmentJourneyId",
                table: "FollowUpReminder");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentJourney_MedicalVisit_VisitId",
                table: "TreatmentJourney");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMedication_Medicine_MedicineId",
                table: "UserMedication");

            migrationBuilder.DropTable(
                name: "DrugAnalysisResult");

            migrationBuilder.DropTable(
                name: "LabResultDetails");

            migrationBuilder.DropTable(
                name: "MedicalRecordFile");

            migrationBuilder.DropTable(
                name: "MedicationScanResult");

            migrationBuilder.DropTable(
                name: "DrugAnalysis");

            migrationBuilder.DropTable(
                name: "LabResult");

            migrationBuilder.DropTable(
                name: "MedicationScan");

            migrationBuilder.DropTable(
                name: "Medicine");

            migrationBuilder.DropTable(
                name: "MedicalRecord");

            migrationBuilder.DropTable(
                name: "MedicalVisit");

            migrationBuilder.DropIndex(
                name: "IX_UserMedication_MedicineId",
                table: "UserMedication");

            migrationBuilder.DropIndex(
                name: "IX_FollowUpReminder_TreatmentJourneyId",
                table: "FollowUpReminder");

            migrationBuilder.DropIndex(
                name: "IX_AISystemConfig_DrugAnalysisId",
                table: "AISystemConfig");

            migrationBuilder.DropIndex(
                name: "IX_AISystemConfig_MedicationScanId",
                table: "AISystemConfig");

            migrationBuilder.DropColumn(
                name: "MedicineId",
                table: "UserMedication");

            migrationBuilder.DropColumn(
                name: "TreatmentJourneyId",
                table: "FollowUpReminder");

            migrationBuilder.DropColumn(
                name: "DrugAnalysisId",
                table: "AISystemConfig");

            migrationBuilder.DropColumn(
                name: "MedicationScanId",
                table: "AISystemConfig");

            migrationBuilder.RenameColumn(
                name: "VisitId",
                table: "TreatmentJourney",
                newName: "FacilityId");

            migrationBuilder.RenameIndex(
                name: "IX_TreatmentJourney_VisitId",
                table: "TreatmentJourney",
                newName: "IX_TreatmentJourney_FacilityId");

            migrationBuilder.RenameColumn(
                name: "VisitId",
                table: "AISystemConfig",
                newName: "TestSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_AISystemConfig_VisitId",
                table: "AISystemConfig",
                newName: "IX_AISystemConfig_TestSessionId");

            migrationBuilder.AddColumn<string>(
                name: "MedicineName",
                table: "UserMedication",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "TreatmentJourney",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SymptomAnalysisSessionId",
                table: "RecoveryPlan",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TestSessionId",
                table: "RecoveryPlan",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConsultationSessionId",
                table: "AIAnalysis",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecoveryPlanId",
                table: "AIAnalysis",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SymptomAnalysisSessionId",
                table: "AIAnalysis",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TestSessionId",
                table: "AIAnalysis",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LabIndicatorMaster",
                columns: table => new
                {
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MinReference = table.Column<double>(type: "double precision", nullable: true),
                    MaxReference = table.Column<double>(type: "double precision", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabIndicatorMaster", x => x.IndicatorId);
                });

            migrationBuilder.CreateTable(
                name: "LabTestSession",
                columns: table => new
                {
                    TestSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawOcrText = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTestSession", x => x.TestSessionId);
                    table.ForeignKey(
                        name: "FK_LabTestSession_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabIndicatorAdviceCache",
                columns: table => new
                {
                    CacheId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PossibleCauses = table.Column<string>(type: "text", nullable: true),
                    LifestyleAdvice = table.Column<string>(type: "text", nullable: true),
                    NutritionalAdvice = table.Column<string>(type: "text", nullable: true),
                    UrgencyLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WarningSigns = table.Column<string>(type: "text", nullable: true),
                    FollowUpSuggestion = table.Column<string>(type: "text", nullable: true),
                    DoctorQuestions = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabIndicatorAdviceCache", x => x.CacheId);
                    table.ForeignKey(
                        name: "FK_LabIndicatorAdviceCache_LabIndicatorMaster_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "LabIndicatorMaster",
                        principalColumn: "IndicatorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabTestResultDetail",
                columns: table => new
                {
                    ResultDetailId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserValue = table.Column<double>(type: "double precision", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTestResultDetail", x => x.ResultDetailId);
                    table.ForeignKey(
                        name: "FK_LabTestResultDetail_LabIndicatorMaster_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "LabIndicatorMaster",
                        principalColumn: "IndicatorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabTestResultDetail_LabTestSession_TestSessionId",
                        column: x => x.TestSessionId,
                        principalTable: "LabTestSession",
                        principalColumn: "TestSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentJourney_DepartmentId",
                table: "TreatmentJourney",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryPlan_SymptomAnalysisSessionId",
                table: "RecoveryPlan",
                column: "SymptomAnalysisSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryPlan_TestSessionId",
                table: "RecoveryPlan",
                column: "TestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIAnalysis_ConsultationSessionId",
                table: "AIAnalysis",
                column: "ConsultationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIAnalysis_RecoveryPlanId",
                table: "AIAnalysis",
                column: "RecoveryPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_AIAnalysis_SymptomAnalysisSessionId",
                table: "AIAnalysis",
                column: "SymptomAnalysisSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIAnalysis_TestSessionId",
                table: "AIAnalysis",
                column: "TestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LabIndicatorAdviceCache_IndicatorId_Status",
                table: "LabIndicatorAdviceCache",
                columns: new[] { "IndicatorId", "Status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabIndicatorMaster_Symbol",
                table: "LabIndicatorMaster",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResultDetail_IndicatorId",
                table: "LabTestResultDetail",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResultDetail_TestSessionId",
                table: "LabTestResultDetail",
                column: "TestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestSession_UserId",
                table: "LabTestSession",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AIAnalysis_ConsultationSession_ConsultationSessionId",
                table: "AIAnalysis",
                column: "ConsultationSessionId",
                principalTable: "ConsultationSession",
                principalColumn: "ConsultationSessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AIAnalysis_LabTestSession_TestSessionId",
                table: "AIAnalysis",
                column: "TestSessionId",
                principalTable: "LabTestSession",
                principalColumn: "TestSessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AIAnalysis_RecoveryPlan_RecoveryPlanId",
                table: "AIAnalysis",
                column: "RecoveryPlanId",
                principalTable: "RecoveryPlan",
                principalColumn: "RecoveryPlanId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AIAnalysis_SymptomAnalysisSession_SymptomAnalysisSessionId",
                table: "AIAnalysis",
                column: "SymptomAnalysisSessionId",
                principalTable: "SymptomAnalysisSession",
                principalColumn: "SymptomAnalysisSessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AISystemConfig_LabTestSession_TestSessionId",
                table: "AISystemConfig",
                column: "TestSessionId",
                principalTable: "LabTestSession",
                principalColumn: "TestSessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RecoveryPlan_LabTestSession_TestSessionId",
                table: "RecoveryPlan",
                column: "TestSessionId",
                principalTable: "LabTestSession",
                principalColumn: "TestSessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RecoveryPlan_SymptomAnalysisSession_SymptomAnalysisSessionId",
                table: "RecoveryPlan",
                column: "SymptomAnalysisSessionId",
                principalTable: "SymptomAnalysisSession",
                principalColumn: "SymptomAnalysisSessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentJourney_MedicalDepartment_DepartmentId",
                table: "TreatmentJourney",
                column: "DepartmentId",
                principalTable: "MedicalDepartment",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentJourney_MedicalFacility_FacilityId",
                table: "TreatmentJourney",
                column: "FacilityId",
                principalTable: "MedicalFacility",
                principalColumn: "FacilityId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AIAnalysis_ConsultationSession_ConsultationSessionId",
                table: "AIAnalysis");

            migrationBuilder.DropForeignKey(
                name: "FK_AIAnalysis_LabTestSession_TestSessionId",
                table: "AIAnalysis");

            migrationBuilder.DropForeignKey(
                name: "FK_AIAnalysis_RecoveryPlan_RecoveryPlanId",
                table: "AIAnalysis");

            migrationBuilder.DropForeignKey(
                name: "FK_AIAnalysis_SymptomAnalysisSession_SymptomAnalysisSessionId",
                table: "AIAnalysis");

            migrationBuilder.DropForeignKey(
                name: "FK_AISystemConfig_LabTestSession_TestSessionId",
                table: "AISystemConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_RecoveryPlan_LabTestSession_TestSessionId",
                table: "RecoveryPlan");

            migrationBuilder.DropForeignKey(
                name: "FK_RecoveryPlan_SymptomAnalysisSession_SymptomAnalysisSessionId",
                table: "RecoveryPlan");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentJourney_MedicalDepartment_DepartmentId",
                table: "TreatmentJourney");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentJourney_MedicalFacility_FacilityId",
                table: "TreatmentJourney");

            migrationBuilder.DropTable(
                name: "LabIndicatorAdviceCache");

            migrationBuilder.DropTable(
                name: "LabTestResultDetail");

            migrationBuilder.DropTable(
                name: "LabIndicatorMaster");

            migrationBuilder.DropTable(
                name: "LabTestSession");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentJourney_DepartmentId",
                table: "TreatmentJourney");

            migrationBuilder.DropIndex(
                name: "IX_RecoveryPlan_SymptomAnalysisSessionId",
                table: "RecoveryPlan");

            migrationBuilder.DropIndex(
                name: "IX_RecoveryPlan_TestSessionId",
                table: "RecoveryPlan");

            migrationBuilder.DropIndex(
                name: "IX_AIAnalysis_ConsultationSessionId",
                table: "AIAnalysis");

            migrationBuilder.DropIndex(
                name: "IX_AIAnalysis_RecoveryPlanId",
                table: "AIAnalysis");

            migrationBuilder.DropIndex(
                name: "IX_AIAnalysis_SymptomAnalysisSessionId",
                table: "AIAnalysis");

            migrationBuilder.DropIndex(
                name: "IX_AIAnalysis_TestSessionId",
                table: "AIAnalysis");

            migrationBuilder.DropColumn(
                name: "MedicineName",
                table: "UserMedication");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "TreatmentJourney");

            migrationBuilder.DropColumn(
                name: "SymptomAnalysisSessionId",
                table: "RecoveryPlan");

            migrationBuilder.DropColumn(
                name: "TestSessionId",
                table: "RecoveryPlan");

            migrationBuilder.DropColumn(
                name: "ConsultationSessionId",
                table: "AIAnalysis");

            migrationBuilder.DropColumn(
                name: "RecoveryPlanId",
                table: "AIAnalysis");

            migrationBuilder.DropColumn(
                name: "SymptomAnalysisSessionId",
                table: "AIAnalysis");

            migrationBuilder.DropColumn(
                name: "TestSessionId",
                table: "AIAnalysis");

            migrationBuilder.RenameColumn(
                name: "FacilityId",
                table: "TreatmentJourney",
                newName: "VisitId");

            migrationBuilder.RenameIndex(
                name: "IX_TreatmentJourney_FacilityId",
                table: "TreatmentJourney",
                newName: "IX_TreatmentJourney_VisitId");

            migrationBuilder.RenameColumn(
                name: "TestSessionId",
                table: "AISystemConfig",
                newName: "VisitId");

            migrationBuilder.RenameIndex(
                name: "IX_AISystemConfig_TestSessionId",
                table: "AISystemConfig",
                newName: "IX_AISystemConfig_VisitId");

            migrationBuilder.AddColumn<Guid>(
                name: "MedicineId",
                table: "UserMedication",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TreatmentJourneyId",
                table: "FollowUpReminder",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DrugAnalysisId",
                table: "AISystemConfig",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MedicationScanId",
                table: "AISystemConfig",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DrugAnalysis",
                columns: table => new
                {
                    DrugAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentJourneyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugAnalysis", x => x.DrugAnalysisId);
                    table.ForeignKey(
                        name: "FK_DrugAnalysis_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugAnalysis_TreatmentJourney_TreatmentJourneyId",
                        column: x => x.TreatmentJourneyId,
                        principalTable: "TreatmentJourney",
                        principalColumn: "TreatmentJourneyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalVisit",
                columns: table => new
                {
                    VisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DiagnosisNote = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalVisit", x => x.VisitId);
                    table.ForeignKey(
                        name: "FK_MedicalVisit_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalVisit_Doctor_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctor",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalVisit_MedicalDepartment_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "MedicalDepartment",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalVisit_MedicalFacility_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "MedicalFacility",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicationScan",
                columns: table => new
                {
                    MedicationScanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExtractedText = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationScan", x => x.MedicationScanId);
                    table.ForeignKey(
                        name: "FK_MedicationScan_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Medicine",
                columns: table => new
                {
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActiveIngredient = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DosageForm = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    MedicineName = table.Column<string>(type: "text", nullable: true),
                    Strength = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicine", x => x.MedicineId);
                });

            migrationBuilder.CreateTable(
                name: "MedicalRecord",
                columns: table => new
                {
                    MedicalRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RecordDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordType = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalRecord", x => x.MedicalRecordId);
                    table.ForeignKey(
                        name: "FK_MedicalRecord_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalRecord_MedicalVisit_VisitId",
                        column: x => x.VisitId,
                        principalTable: "MedicalVisit",
                        principalColumn: "VisitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrugAnalysisResult",
                columns: table => new
                {
                    DrugAnalysisResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombinationVerdict = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DietaryInteraction = table.Column<string>(type: "text", nullable: true),
                    InteractionDetail = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Precaution = table.Column<string>(type: "text", nullable: true),
                    Severity = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsageOptimization = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugAnalysisResult", x => x.DrugAnalysisResultId);
                    table.ForeignKey(
                        name: "FK_DrugAnalysisResult_DrugAnalysis_DrugAnalysisId",
                        column: x => x.DrugAnalysisId,
                        principalTable: "DrugAnalysis",
                        principalColumn: "DrugAnalysisId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrugAnalysisResult_Medicine_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicine",
                        principalColumn: "MedicineId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicationScanResult",
                columns: table => new
                {
                    MedicationScanResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationScanId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DetectedDosage = table.Column<string>(type: "text", nullable: true),
                    DetectedName = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationScanResult", x => x.MedicationScanResultId);
                    table.ForeignKey(
                        name: "FK_MedicationScanResult_MedicationScan_MedicationScanId",
                        column: x => x.MedicationScanId,
                        principalTable: "MedicationScan",
                        principalColumn: "MedicationScanId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicationScanResult_Medicine_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicine",
                        principalColumn: "MedicineId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabResult",
                columns: table => new
                {
                    LabResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicalRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LabName = table.Column<string>(type: "text", nullable: true),
                    OverallConclusion = table.Column<string>(type: "text", nullable: true),
                    TestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResult", x => x.LabResultId);
                    table.ForeignKey(
                        name: "FK_LabResult_MedicalRecord_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecord",
                        principalColumn: "MedicalRecordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalRecordFile",
                columns: table => new
                {
                    MedicalRecordFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicalRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FileType = table.Column<string>(type: "text", nullable: true),
                    FileUrl = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalRecordFile", x => x.MedicalRecordFileId);
                    table.ForeignKey(
                        name: "FK_MedicalRecordFile_MedicalRecord_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecord",
                        principalColumn: "MedicalRecordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabResultDetails",
                columns: table => new
                {
                    LabResultItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Interpretation = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    NormalRangeMax = table.Column<double>(type: "double precision", nullable: true),
                    NormalRangeMin = table.Column<double>(type: "double precision", nullable: true),
                    TestName = table.Column<string>(type: "text", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResultDetails", x => x.LabResultItemId);
                    table.ForeignKey(
                        name: "FK_LabResultDetails_LabResult_LabResultId",
                        column: x => x.LabResultId,
                        principalTable: "LabResult",
                        principalColumn: "LabResultId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMedication_MedicineId",
                table: "UserMedication",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpReminder_TreatmentJourneyId",
                table: "FollowUpReminder",
                column: "TreatmentJourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_AISystemConfig_DrugAnalysisId",
                table: "AISystemConfig",
                column: "DrugAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_AISystemConfig_MedicationScanId",
                table: "AISystemConfig",
                column: "MedicationScanId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAnalysis_TreatmentJourneyId",
                table: "DrugAnalysis",
                column: "TreatmentJourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAnalysis_UserId",
                table: "DrugAnalysis",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAnalysisResult_DrugAnalysisId",
                table: "DrugAnalysisResult",
                column: "DrugAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAnalysisResult_MedicineId",
                table: "DrugAnalysisResult",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResult_MedicalRecordId",
                table: "LabResult",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultDetails_LabResultId",
                table: "LabResultDetails",
                column: "LabResultId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecord_UserId",
                table: "MedicalRecord",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecord_VisitId",
                table: "MedicalRecord",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecordFile_MedicalRecordId",
                table: "MedicalRecordFile",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisit_DepartmentId",
                table: "MedicalVisit",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisit_DoctorId",
                table: "MedicalVisit",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisit_FacilityId",
                table: "MedicalVisit",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisit_UserId",
                table: "MedicalVisit",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationScan_UserId",
                table: "MedicationScan",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationScanResult_MedicationScanId",
                table: "MedicationScanResult",
                column: "MedicationScanId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationScanResult_MedicineId",
                table: "MedicationScanResult",
                column: "MedicineId");

            migrationBuilder.AddForeignKey(
                name: "FK_AISystemConfig_DrugAnalysis_DrugAnalysisId",
                table: "AISystemConfig",
                column: "DrugAnalysisId",
                principalTable: "DrugAnalysis",
                principalColumn: "DrugAnalysisId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AISystemConfig_MedicalVisit_VisitId",
                table: "AISystemConfig",
                column: "VisitId",
                principalTable: "MedicalVisit",
                principalColumn: "VisitId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AISystemConfig_MedicationScan_MedicationScanId",
                table: "AISystemConfig",
                column: "MedicationScanId",
                principalTable: "MedicationScan",
                principalColumn: "MedicationScanId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FollowUpReminder_TreatmentJourney_TreatmentJourneyId",
                table: "FollowUpReminder",
                column: "TreatmentJourneyId",
                principalTable: "TreatmentJourney",
                principalColumn: "TreatmentJourneyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentJourney_MedicalVisit_VisitId",
                table: "TreatmentJourney",
                column: "VisitId",
                principalTable: "MedicalVisit",
                principalColumn: "VisitId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMedication_Medicine_MedicineId",
                table: "UserMedication",
                column: "MedicineId",
                principalTable: "Medicine",
                principalColumn: "MedicineId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
