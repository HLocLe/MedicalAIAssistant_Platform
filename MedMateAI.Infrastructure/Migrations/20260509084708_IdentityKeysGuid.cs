using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMateAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IdentityKeysGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL: convert Identity keys text -> uuid while preserving data.
            // Requires dropping FKs first, then ALTER COLUMN ... TYPE uuid USING (...::uuid), then re-adding FKs.

            migrationBuilder.Sql("""
                ALTER TABLE "AspNetUserClaims" DROP CONSTRAINT IF EXISTS "FK_AspNetUserClaims_AspNetUsers_UserId";
                ALTER TABLE "AspNetUserLogins" DROP CONSTRAINT IF EXISTS "FK_AspNetUserLogins_AspNetUsers_UserId";
                ALTER TABLE "AspNetUserRoles" DROP CONSTRAINT IF EXISTS "FK_AspNetUserRoles_AspNetUsers_UserId";
                ALTER TABLE "AspNetUserTokens" DROP CONSTRAINT IF EXISTS "FK_AspNetUserTokens_AspNetUsers_UserId";
                ALTER TABLE "AspNetUserRoles" DROP CONSTRAINT IF EXISTS "FK_AspNetUserRoles_AspNetRoles_RoleId";
                ALTER TABLE "AspNetRoleClaims" DROP CONSTRAINT IF EXISTS "FK_AspNetRoleClaims_AspNetRoles_RoleId";
                ALTER TABLE "RefreshTokens" DROP CONSTRAINT IF EXISTS "FK_RefreshTokens_AspNetUsers_UserId";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "AspNetUsers" ALTER COLUMN "Id" TYPE uuid USING ("Id"::uuid);
                ALTER TABLE "AspNetRoles" ALTER COLUMN "Id" TYPE uuid USING ("Id"::uuid);

                ALTER TABLE "AspNetUserClaims" ALTER COLUMN "UserId" TYPE uuid USING ("UserId"::uuid);
                ALTER TABLE "AspNetUserLogins" ALTER COLUMN "UserId" TYPE uuid USING ("UserId"::uuid);
                ALTER TABLE "AspNetUserRoles" ALTER COLUMN "UserId" TYPE uuid USING ("UserId"::uuid);
                ALTER TABLE "AspNetUserRoles" ALTER COLUMN "RoleId" TYPE uuid USING ("RoleId"::uuid);
                ALTER TABLE "AspNetUserTokens" ALTER COLUMN "UserId" TYPE uuid USING ("UserId"::uuid);
                ALTER TABLE "AspNetRoleClaims" ALTER COLUMN "RoleId" TYPE uuid USING ("RoleId"::uuid);

                ALTER TABLE "RefreshTokens" ALTER COLUMN "UserId" TYPE uuid USING ("UserId"::uuid);
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "AspNetRoleClaims"
                    ADD CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId"
                    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE;

                ALTER TABLE "AspNetUserClaims"
                    ADD CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE;

                ALTER TABLE "AspNetUserLogins"
                    ADD CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE;

                ALTER TABLE "AspNetUserRoles"
                    ADD CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId"
                    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE;

                ALTER TABLE "AspNetUserRoles"
                    ADD CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE;

                ALTER TABLE "AspNetUserTokens"
                    ADD CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE;

                ALTER TABLE "RefreshTokens"
                    ADD CONSTRAINT "FK_RefreshTokens_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "AspNetUserClaims" DROP CONSTRAINT IF EXISTS "FK_AspNetUserClaims_AspNetUsers_UserId";
                ALTER TABLE "AspNetUserLogins" DROP CONSTRAINT IF EXISTS "FK_AspNetUserLogins_AspNetUsers_UserId";
                ALTER TABLE "AspNetUserRoles" DROP CONSTRAINT IF EXISTS "FK_AspNetUserRoles_AspNetUsers_UserId";
                ALTER TABLE "AspNetUserTokens" DROP CONSTRAINT IF EXISTS "FK_AspNetUserTokens_AspNetUsers_UserId";
                ALTER TABLE "AspNetUserRoles" DROP CONSTRAINT IF EXISTS "FK_AspNetUserRoles_AspNetRoles_RoleId";
                ALTER TABLE "AspNetRoleClaims" DROP CONSTRAINT IF EXISTS "FK_AspNetRoleClaims_AspNetRoles_RoleId";
                ALTER TABLE "RefreshTokens" DROP CONSTRAINT IF EXISTS "FK_RefreshTokens_AspNetUsers_UserId";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "AspNetUsers" ALTER COLUMN "Id" TYPE text USING ("Id"::text);
                ALTER TABLE "AspNetRoles" ALTER COLUMN "Id" TYPE text USING ("Id"::text);

                ALTER TABLE "AspNetUserClaims" ALTER COLUMN "UserId" TYPE text USING ("UserId"::text);
                ALTER TABLE "AspNetUserLogins" ALTER COLUMN "UserId" TYPE text USING ("UserId"::text);
                ALTER TABLE "AspNetUserRoles" ALTER COLUMN "UserId" TYPE text USING ("UserId"::text);
                ALTER TABLE "AspNetUserRoles" ALTER COLUMN "RoleId" TYPE text USING ("RoleId"::text);
                ALTER TABLE "AspNetUserTokens" ALTER COLUMN "UserId" TYPE text USING ("UserId"::text);
                ALTER TABLE "AspNetRoleClaims" ALTER COLUMN "RoleId" TYPE text USING ("RoleId"::text);

                ALTER TABLE "RefreshTokens" ALTER COLUMN "UserId" TYPE character varying(450) USING ("UserId"::text);
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "AspNetRoleClaims"
                    ADD CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId"
                    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE;

                ALTER TABLE "AspNetUserClaims"
                    ADD CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE;

                ALTER TABLE "AspNetUserLogins"
                    ADD CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE;

                ALTER TABLE "AspNetUserRoles"
                    ADD CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId"
                    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE;

                ALTER TABLE "AspNetUserRoles"
                    ADD CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE;

                ALTER TABLE "AspNetUserTokens"
                    ADD CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE;

                ALTER TABLE "RefreshTokens"
                    ADD CONSTRAINT "FK_RefreshTokens_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE;
                """);
        }
    }
}
