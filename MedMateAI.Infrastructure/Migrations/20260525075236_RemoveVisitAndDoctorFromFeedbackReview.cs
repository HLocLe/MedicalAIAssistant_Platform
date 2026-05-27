using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMateAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVisitAndDoctorFromFeedbackReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeedbackReview_Doctor_DoctorId",
                table: "FeedbackReview");

            migrationBuilder.DropForeignKey(
                name: "FK_FeedbackReview_MedicalVisit_VisitId",
                table: "FeedbackReview");

            migrationBuilder.DropIndex(
                name: "IX_FeedbackReview_DoctorId",
                table: "FeedbackReview");

            migrationBuilder.DropIndex(
                name: "IX_FeedbackReview_VisitId",
                table: "FeedbackReview");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "FeedbackReview");

            migrationBuilder.DropColumn(
                name: "VisitId",
                table: "FeedbackReview");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                table: "FeedbackReview",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "VisitId",
                table: "FeedbackReview",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackReview_DoctorId",
                table: "FeedbackReview",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackReview_VisitId",
                table: "FeedbackReview",
                column: "VisitId");

            migrationBuilder.AddForeignKey(
                name: "FK_FeedbackReview_Doctor_DoctorId",
                table: "FeedbackReview",
                column: "DoctorId",
                principalTable: "Doctor",
                principalColumn: "DoctorId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FeedbackReview_MedicalVisit_VisitId",
                table: "FeedbackReview",
                column: "VisitId",
                principalTable: "MedicalVisit",
                principalColumn: "VisitId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
