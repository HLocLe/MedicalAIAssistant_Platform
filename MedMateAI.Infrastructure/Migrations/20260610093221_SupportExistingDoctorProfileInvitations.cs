using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMateAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupportExistingDoctorProfileInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                table: "DoctorInvitation",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorInvitation_DoctorId",
                table: "DoctorInvitation",
                column: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorInvitation_Doctor_DoctorId",
                table: "DoctorInvitation",
                column: "DoctorId",
                principalTable: "Doctor",
                principalColumn: "DoctorId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorInvitation_Doctor_DoctorId",
                table: "DoctorInvitation");

            migrationBuilder.DropIndex(
                name: "IX_DoctorInvitation_DoctorId",
                table: "DoctorInvitation");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "DoctorInvitation");
        }
    }
}
