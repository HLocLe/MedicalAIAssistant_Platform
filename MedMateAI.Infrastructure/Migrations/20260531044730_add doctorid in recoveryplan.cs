using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMateAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class adddoctoridinrecoveryplan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalNote",
                table: "TreatmentJourney",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "TreatmentJourney",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "TreatmentJourney",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                table: "TreatmentJourney",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentJourney_DoctorId",
                table: "TreatmentJourney",
                column: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentJourney_Doctor_DoctorId",
                table: "TreatmentJourney",
                column: "DoctorId",
                principalTable: "Doctor",
                principalColumn: "DoctorId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentJourney_Doctor_DoctorId",
                table: "TreatmentJourney");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentJourney_DoctorId",
                table: "TreatmentJourney");

            migrationBuilder.DropColumn(
                name: "ApprovalNote",
                table: "TreatmentJourney");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "TreatmentJourney");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "TreatmentJourney");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "TreatmentJourney");
        }
    }
}
